using InventoryApp.Data;
using Microsoft.Data.Sqlite;
using System.Data;
using System.Diagnostics;
using System.Net;
using System.Text;
using Humanizer;
using System.Globalization;

namespace InventoryApp
{
    public partial class Form1 : Form
    {

        //les formulaires embarqués pour les onglets de tabpages
        private FrmGererModeles _frmModeleEmbed;
        private FrmGererCategories _frmCategorieEmbed;
        private FrmGererMarques _frmMarqueEmbed;


        // Colonnes filtrables centralisées


        private static readonly (string Affichage, string Colonne)[] ColonnesFiltrablesMVM = new[]
        {
            ("Tous les champs", ""),
            ("N° Bon",           "N° Bon"),
            ("Type",             "Type"),
            ("Employé",          "Employé"),
            ("Département",      "Département"),
            ("Remarque",         "Remarque")
        };

        private static readonly (string Affichage, string Colonne)[] ColonnesFiltrablesINV = new[]
        {
            ("Tous les champs", ""),
            ("Structure",        "Structure"),
            ("Bureau",            "Bureau"),
            ("Date",              "Date"),
        };

        public Form1()
        {
            InitializeComponent();

            // Sauvegarde automatique et silencieuse juste avant la fermeture de l'application —
            // aucune action de l'utilisateur nécessaire, capture toujours le dernier état des données.
            this.FormClosing += (s, e) => InventoryApp.Data.DatabaseHelper.SauvegarderBaseDonnees();


            // Configuration DataGridView Mouvements
            tableMVMDataGridView.RowTemplate.Height = 38;
            tableMVMDataGridView.CellPainting += TableMVMDataGridView_CellPainting;
            tableMVMDataGridView.CellClick += tableMVMDataGridView_CellClick;
            tableMVMDataGridView.CellMouseMove += (s, e) => tableMVMDataGridView.InvalidateCell(e.ColumnIndex, e.RowIndex);
            tableMVMDataGridView.CellMouseLeave += (s, e) => tableMVMDataGridView.InvalidateCell(e.ColumnIndex, e.RowIndex);

            ChargerMouvements();

            // Événements
            filtreTableMVMTextBox.TextChanged += filtreTableMVMTextBox_TextChanged;
            listeDeFiltrageMVMComboBox.SelectedIndexChanged += listeDeFiltrageMVMComboBox_SelectedIndexChanged;


            // Configuration DataGridView Inventaires
            tableINVDataGridView.RowTemplate.Height = 38;
            tableINVDataGridView.CellPainting += TableINVDataGridView_CellPainting;
            tableINVDataGridView.CellClick += tableINVDataGridView_CellClick;
            tableINVDataGridView.CellMouseMove += (s, e) => tableINVDataGridView.InvalidateCell(e.ColumnIndex, e.RowIndex);
            tableINVDataGridView.CellMouseLeave += (s, e) => tableINVDataGridView.InvalidateCell(e.ColumnIndex, e.RowIndex);

            ChargerInventaires();

            filtreTableINVTextBox.TextChanged += filtreTableINVTextBox_TextChanged;
            listeDeFiltrageINVComboBox.SelectedIndexChanged += listeDeFiltrageINVComboBox_SelectedIndexChanged;

            AfficherInfoUtilisateur();
            AfficherConteneur(home_container);
            OuvrirFormulaireEnfant(CreerFrmAccueil());

            //TextBoxfiltrage.KeyDown += TextBoxfiltrage_KeyDown;
            stock_containers.SelectedIndexChanged += stock_containers_SelectedIndexChanged;
        }

        // =====================================================
        // SECTION 1 : OUTILS COMMUNS DES GRILLES (boutons d'action)
        // =====================================================

        private void DessinerBoutonAction(DataGridViewCellPaintingEventArgs e, bool isHovered, bool isClicked,
            Color bg, Color bgHover, Color bgClick, Color borderColor, string iconFilename, int iconSize)
        {
            e.PaintBackground(e.CellBounds, true);

            Color currentBg = isClicked ? bgClick : (isHovered ? bgHover : bg);
            Rectangle btnRect = new Rectangle(e.CellBounds.Left + 4, e.CellBounds.Top + 4, e.CellBounds.Width - 8, e.CellBounds.Height - 8);

            using (var brush = new SolidBrush(currentBg))
                e.Graphics.FillRectangle(brush, btnRect);

            using (var pen = new Pen(borderColor))
                e.Graphics.DrawRectangle(pen, btnRect);

            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "image", iconFilename);
            if (!File.Exists(path)) path = Path.Combine("image", iconFilename);

            if (File.Exists(path))
            {
                using (Image img = Image.FromFile(path))
                {
                    int x = btnRect.Left + (btnRect.Width - iconSize) / 2;
                    int y = btnRect.Top + (btnRect.Height - iconSize) / 2;
                    e.Graphics.DrawImage(img, new Rectangle(x, y, iconSize, iconSize));
                }
            }
            e.Handled = true;
        }



        // =====================================================
        // SECTION 3 : IMPRESSION GLOBALE & NAVIGATION
        // =====================================================


        private void AfficherConteneur(Control conteneurActif)
        {
            // On masque/affiche les 3 conteneurs principaux du premier niveau
            home_container.Visible = ReferenceEquals(conteneurActif, home_container);
            stock_container.Visible = ReferenceEquals(conteneurActif, stock_container);
            repots_container.Visible = ReferenceEquals(conteneurActif, repots_container);

            // On s'assure que le conteneur sélectionné est ramené au premier plan
            conteneurActif.BringToFront();
        }

        private void btnToAccueilcontainer_Click(object sender, EventArgs e)
        {
            AfficherConteneur(home_container);
            OuvrirFormulaireEnfant(CreerFrmAccueil());
        }


        // Instancie FrmAccueil et câble systématiquement tous ses événements.
        // Utilisée à la fois au démarrage (constructeur) et à chaque retour sur "Accueil",
        // pour que les boutons Modifier/Supprimer/Imprimer fonctionnent dès le premier clic.
        private FrmAccueil CreerFrmAccueil()
        {
            var frmAccueil = new FrmAccueil(this);

            frmAccueil.OnNouveauMouvementClicked += (s, ev) => btnNouveauMouvement_Click(s, ev);
            frmAccueil.OnInventaireClicked += (s, ev) => btnNouveauInventaire_Click(s, ev);
            frmAccueil.OnEditEmployeRequested += FrmAccueil_OnEditEmployeRequested;
            frmAccueil.OnDeleteEmployeRequested += FrmAccueil_OnDeleteEmployeRequested;

            return frmAccueil;
        }

        // Cette méthode est appelée quand l'utilisateur clique sur "Modifier" un employé dans FrmAccueil
        private void FrmAccueil_OnEditEmployeRequested(object sender, int idEmploye)
        {
            // On rouvre EXACTEMENT le même formulaire, mais en passant l'id :
            // -> le constructeur bascule automatiquement en mode "Modification"
            using (var frm = new FrmAjouterEmploye(idEmploye))
            {
                if (frm.ShowDialog(this) == DialogResult.OK)
                {
                    // Rafraîchir la liste des employés affichée dans FrmAccueil
                    if (home_container.Tag is FrmAccueil accueilOuvert)
                        accueilOuvert.ChargerResumeEmployes();
                }
            }
        }

        // Cette méthode sera appelée quand l'utilisateur clique sur "Supprimer" un employé dans FrmAccueil
        // Cette méthode est appelée quand l'utilisateur clique sur "Supprimer" un employé dans FrmAccueil
        private void FrmAccueil_OnDeleteEmployeRequested(object sender, int idEmploye)
        {
            // La confirmation a déjà été demandée dans FrmAccueil ; ici on exécute réellement la suppression
            try
            {
                DatabaseHelper.ExecuteNonQuery("DELETE FROM Employe WHERE id = @id", new SqliteParameter("@id", idEmploye));

                if (home_container.Tag is FrmAccueil accueilOuvert)
                    accueilOuvert.ChargerResumeEmployes();
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
            {
                MessageBox.Show(
                    "Impossible de supprimer : cet employé est référencé dans un historique de mouvements.",
                    "Suppression refusée", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }




        private void OuvrirFormulaireEnfant(Form formEnfant)
        {
            var ancien = home_container.Tag as Form;
            home_container.Controls.Clear();
            ancien?.Close(); // arrête les timers de l'ancien tableau de bord (OnFormClosed) au lieu de le laisser tourner
            formEnfant.TopLevel = false;
            formEnfant.FormBorderStyle = FormBorderStyle.None;
            formEnfant.Dock = DockStyle.Fill;
            home_container.Controls.Add(formEnfant);
            home_container.Tag = formEnfant;
            formEnfant.Show();
        }

        private void btnToStockcontainer_Click(object sender, EventArgs e)
        {
            AfficherConteneur(stock_container);
            ChargerOngletActifStock();
        }

        // Recharge les données de l'onglet Stock actuellement affiché (Modèles, Catégories ou Marques).
        // Centralise la logique utilisée à la fois à l'entrée dans le conteneur Stock
        // (où le TabControl ne déclenche pas SelectedIndexChanged si l'onglet actif ne change pas)
        // et lors d'un vrai changement d'onglet, pour garantir que les 3 pages se rechargent toujours.
        private void ChargerOngletActifStock()
        {
            if (stock_containers.SelectedTab == Modèlles)
            {
                ChargerFormulaireModeleDansTab();
            }
            else if (stock_containers.SelectedTab == Categories)
            {
                ChargerFormulaireCategorieDansTab();
            }
            else if (stock_containers.SelectedTab == Marques)
            {
                ChargerFormulaireMarqueDansTab();
            }
        }

        private void btnToRaportscontaine_Click(object sender, EventArgs e)
        {
            AfficherConteneur(repots_container);
        }


        // =====================================================
        // SECTION 4 : MOUVEMENTS (BONS)
        // =====================================================

        public void ChargerMouvements()
        {
            string sql = @"
                SELECT 
                    m.id AS 'ID',
                    COALESCE(m.code_mouvement, 'BON-' || m.id) AS 'N° Bon',
                    m.nom AS 'Nom Mouvement',
                    m.reference AS 'Référence',
                    m.type_mouvement AS 'Type',
                    m.date_mouvement AS 'Date',
                    (emp.nom || ' ' || emp.prenom) AS 'Employé',
                    COALESCE(emp.departement, 'Sans Service') AS 'Département',
                    COALESCE(emp.function, 'Emp') AS 'Fonction',
                    m.observation AS 'Remarque',
                    m.contenu AS 'Contenu'
                FROM Mouvement m
                LEFT JOIN Employe emp ON m.employe_id = emp.id
                ORDER BY m.id DESC";

            DataTable dt = DatabaseHelper.ExecuteQuery(sql);
            tableMVMDataGridView.AutoGenerateColumns = true;
            tableMVMDataGridView.DataSource = dt;
            //pour masquer le contenu dans la table
            if (tableMVMDataGridView.Columns.Contains("Contenu"))
            {
                tableMVMDataGridView.Columns["Contenu"].Visible = false;
            }
            if (tableMVMDataGridView.Columns.Contains("Fonction"))
            {
                tableMVMDataGridView.Columns["Fonction"].Visible = false;
            }

            // 1. Réinitialiser la source et vider les colonnes préexistantes du Designer
            tableMVMDataGridView.DataSource = null;
            tableMVMDataGridView.Columns.Clear();

            // 2. Générer automatiquement les colonnes texte
            tableMVMDataGridView.AutoGenerateColumns = true;
            tableMVMDataGridView.DataSource = dt;

            // 3. Masquer les colonnes techniques
            if (tableMVMDataGridView.Columns.Contains("Contenu"))
            {
                tableMVMDataGridView.Columns["Contenu"].Visible = false;
            }
            if (tableMVMDataGridView.Columns.Contains("Fonction"))
            {
                tableMVMDataGridView.Columns["Fonction"].Visible = false;
            }

            // 4. Ajouter les boutons d'action
            AjouterColonnesActionsMVM();
            PeuplerListeFiltrageMVM();

        }
        private void AjouterColonnesActionsMVM()
        {
            string[] colsActions = { "colModifierMVM", "colSupprimerMVM", "colImprimerMVM" };
            foreach (var colName in colsActions)
            {
                if (tableMVMDataGridView.Columns.Contains(colName))
                    tableMVMDataGridView.Columns.Remove(colName);
            }

            tableMVMDataGridView.Columns.Add(new DataGridViewButtonColumn
            {
                Name = "colModifierMVM",
                HeaderText = "Modifier",
                Width = 60,
                FlatStyle = FlatStyle.Flat
            });

            tableMVMDataGridView.Columns.Add(new DataGridViewButtonColumn
            {
                Name = "colSupprimerMVM",
                HeaderText = "Supprimer",
                Width = 60,
                FlatStyle = FlatStyle.Flat
            });

            tableMVMDataGridView.Columns.Add(new DataGridViewButtonColumn
            {
                Name = "colImprimerMVM",
                HeaderText = "Imprimer",
                Width = 60,
                FlatStyle = FlatStyle.Flat
            });
        }

        private void TableMVMDataGridView_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0) return;

            Point mousePos = tableMVMDataGridView.PointToClient(Cursor.Position);
            bool isHovered = e.CellBounds.Contains(mousePos);
            bool isClicked = isHovered && (Control.MouseButtons == MouseButtons.Left);
            int iconSize = 18;

            if (e.ColumnIndex == tableMVMDataGridView.Columns["colModifierMVM"]?.Index)
            {
                DessinerBoutonAction(e, isHovered, isClicked,
                    Color.FromArgb(240, 253, 244), Color.FromArgb(220, 252, 231), Color.FromArgb(187, 247, 208),
                    Color.FromArgb(134, 239, 172), "pencil_icon.png", iconSize);
            }
            else if (e.ColumnIndex == tableMVMDataGridView.Columns["colSupprimerMVM"]?.Index)
            {
                DessinerBoutonAction(e, isHovered, isClicked,
                    Color.FromArgb(254, 242, 242), Color.FromArgb(254, 226, 226), Color.FromArgb(254, 202, 202),
                    Color.FromArgb(252, 165, 165), "delet_icon.png", iconSize);
            }
            else if (e.ColumnIndex == tableMVMDataGridView.Columns["colImprimerMVM"]?.Index)
            {
                DessinerBoutonAction(e, isHovered, isClicked,
                    Color.FromArgb(239, 246, 255), // Normal (Bleu très doux)
                    Color.FromArgb(219, 234, 254), // Hover (Bleu clair)
                    Color.FromArgb(191, 219, 254), // Click (Bleu moyen)
                    Color.FromArgb(147, 197, 253), // Bordure (Bleu pastel)
                    "imprimerbleu.png",
                    iconSize);
            }
        }

        private void tableMVMDataGridView_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            var grid = (DataGridView)sender;
            string colName = grid.Columns[e.ColumnIndex].Name;
            int mouvementId = Convert.ToInt32(grid.Rows[e.RowIndex].Cells["ID"].Value);

            if (colName == "colModifierMVM")
            {
                using (var frm = new FrmAjouterMouvement(this, mouvementId))
                {
                    if (frm.ShowDialog(this) == DialogResult.OK)
                    {
                        ChargerMouvements();
                        RafraichirAccueil();
                    }
                }
            }
            else if (colName == "colSupprimerMVM")
            {
                string bonNo = grid.Rows[e.RowIndex].Cells["N° Bon"].Value?.ToString() ?? "";
                var confirm = MessageBox.Show(
                    $"Voulez-vous vraiment supprimer le bon de mouvement {bonNo} ?\nCette action supprimera également ses lignes de mouvement associées.",
                    "Confirmer la suppression", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                if (confirm == DialogResult.Yes)
                {
                    try
                    {
                        SupprimerMouvement(mouvementId);
                        ChargerMouvements();
                        RafraichirAccueil();
                    }
                    catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
                    {
                        // Les triggers de la base recalculent Modele.quantite à la suppression des lignes.
                        // Erreur 19 = contrainte violée : le stock d'un modèle deviendrait négatif.
                        MessageBox.Show(
                            "Suppression impossible : annuler ce bon rendrait le stock d'un modèle négatif\n" +
                            "(des quantités entrées par ce bon ont déjà été sorties par un autre mouvement).\n\n" +
                            "Aucune donnée n'a été modifiée.",
                            "Suppression refusée", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Erreur lors de la suppression du mouvement : {ex.Message}",
                            "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            else if (colName == "colImprimerMVM")
            {
                ImprimerBonMouvement(grid.Rows[e.RowIndex]);
            }
        }

        // Suppression atomique d'un bon. Les triggers de la base restituent le stock ligne par ligne, et le
        // CHECK (quantite >= 0) est évalué après chaque ligne : on supprime donc d'abord les SORTIES (le stock
        // remonte), puis les ENTRÉES (il redescend). Sinon un bon valide pouvait être refusé à tort.
        // Tout est dans une seule transaction : en cas d'erreur, rien n'est supprimé.
        private static void SupprimerMouvement(int mouvementId)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                if (conn.State != ConnectionState.Open)
                    conn.Open();

                using (var tx = conn.BeginTransaction())
                {
                    string[] etapes =
                    {
                        "DELETE FROM Ligne_mouvement WHERE mouvement_id = @id AND est_sortie = 1;",
                        "DELETE FROM Ligne_mouvement WHERE mouvement_id = @id;",
                        "DELETE FROM Mouvement WHERE id = @id;"
                    };

                    foreach (string sql in etapes)
                    {
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.Transaction = tx;
                            cmd.CommandText = sql;
                            cmd.Parameters.AddWithValue("@id", mouvementId);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    tx.Commit();
                }
            }
        }

        // ============================================================
        // OUTILS : RÉFÉRENCE AUTOMATIQUE + QUANTITÉ EN LETTRES
        // ============================================================

        // Une référence générée automatiquement commence par "AUTO-" (ex. AUTO-00006)
        private static bool EstReferenceAutomatique(string reference)
        {
            return !string.IsNullOrWhiteSpace(reference) &&
                   reference.Trim().StartsWith("AUTO-", StringComparison.OrdinalIgnoreCase);
        }

        // "12" -> "Douze (12)"
        private static string FormaterQuantiteEnLettres(string quantite)
        {
            if (string.IsNullOrWhiteSpace(quantite)) return "";

            if (!long.TryParse(quantite.Trim(), out long n) || n < 0)
                return quantite.Trim();

            string lettres = n.ToWords(new CultureInfo("fr-FR"));

            return char.ToUpper(lettres[0]) + lettres.Substring(1) + " (" + n + ")";
        }
        
        private void ImprimerBonMouvement(DataGridViewRow row)
        {
            // ============================================================
            // 1. INFORMATIONS DU MOUVEMENT
            // ============================================================

            int mouvementId = Convert.ToInt32(row.Cells["ID"].Value);

            string refMouvement =
                row.Cells["Référence"].Value?.ToString()
                ?? row.Cells["N° Bon"].Value?.ToString()
                ?? "";

            string nomMouvement =
                row.Cells["Nom Mouvement"].Value?.ToString()
                ?? "وصل استلام";

            string nomEmploye =
                row.Cells["Employé"].Value?.ToString()
                ?? "";

            string deptEmploye =
                row.Cells["Département"].Value?.ToString()
                ?? "";

            string fonctionEmploye =
                row.Cells["Fonction"].Value?.ToString()
                ?? "";

            string dateMouvement =
                row.Cells["Date"].Value?.ToString()
                ?? "";

            string obsMouvement =
                row.Cells["Remarque"].Value?.ToString()
                ?? "";

            string contenuMouvement =
                row.Cells["Contenu"].Value?.ToString()
                ?? "";

            if (string.IsNullOrWhiteSpace(contenuMouvement))
            {
                contenuMouvement =
                    "أصرح بأني استلمت من السيد(ة) المكلف(ة) بتسيير مكتب الوسائل العامة والمخزن بمديرية المواصلات السلكية واللاسلكية، العتاد المبين أدناه:";
            }

            // ============================================================
            // 2. RÉCUPÉRATION DES LIGNES DE MOUVEMENT
            // ============================================================

            var lignesSortie =
                new List<Dictionary<string, string>>();

            var lignesEntree =
                new List<Dictionary<string, string>>();

            string sqlLignes = @"
                                    SELECT
                                        lm.est_sortie,
                                        lm.etat_a_la_mouvement,
                                        lm.observation AS obs_ligne,
                                        lm.quantite,

                                        mod.designation AS designation_modele,
                                        mod.reference AS reference_modele,

                                        mrq.designation AS marque_nom,
                                        cat.designation AS famille_nom

                                    FROM Ligne_mouvement lm

                                    INNER JOIN Modele mod
                                        ON lm.modele_id = mod.id

                                    LEFT JOIN Marque mrq
                                        ON mod.marque_id = mrq.id

                                    LEFT JOIN Categorie cat
                                        ON mod.categorie_id = cat.id

                                    WHERE lm.mouvement_id = @id
                                ";

            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                using (var cmd = new Microsoft.Data.Sqlite.SqliteCommand(
                    sqlLignes,
                    conn))
                {
                    cmd.Parameters.AddWithValue("@id", mouvementId);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var item =
                                new Dictionary<string, string>();

                            item["famille_nom"] =
                                reader["famille_nom"] == DBNull.Value
                                    ? ""
                                    : reader["famille_nom"].ToString();

                            item["marque_nom"] =
                                reader["marque_nom"] == DBNull.Value
                                    ? ""
                                    : reader["marque_nom"].ToString();

                            item["designation_modele"] =
                                reader["designation_modele"] == DBNull.Value
                                    ? ""
                                    : reader["designation_modele"].ToString();

                            item["reference_modele"] =
                                reader["reference_modele"] == DBNull.Value
                                    ? ""
                                    : reader["reference_modele"].ToString();

                            item["quantite"] =
                                reader["quantite"] == DBNull.Value
                                    ? ""
                                    : reader["quantite"].ToString();

                            item["etat_a_la_mouvement"] =
                                reader["etat_a_la_mouvement"] == DBNull.Value
                                    ? ""
                                    : reader["etat_a_la_mouvement"].ToString();

                            item["obs_ligne"] =
                                reader["obs_ligne"] == DBNull.Value
                                    ? ""
                                    : reader["obs_ligne"].ToString();

                            bool estSortie = false;

                            if (reader["est_sortie"] != DBNull.Value)
                            {
                                estSortie =
                                    Convert.ToInt32(reader["est_sortie"]) == 1;
                            }

                            if (estSortie)
                            {
                                lignesSortie.Add(item);
                            }
                            else
                            {
                                lignesEntree.Add(item);
                            }
                        }
                    }
                }
            }

            // ============================================================
            // 3. DOCUMENT HTML ET STYLE
            // ============================================================

            var html = new StringBuilder();

            html.Append(@"
                            <!DOCTYPE html>
                            <html dir='ltr' lang='fr'>
                            <head>
                            <meta charset='utf-8'>

                            <style>

                                @page {
                                    size: A4;
                                    margin: 18mm;
                                }

                                body {
                                    font-family: Arial, 'Times New Roman', sans-serif;
                                    margin: 0;
                                    color: #000;
                                    direction: ltr;
                                    text-align: left;
                                    font-size: 14px;
                                }

                                @media screen {
                                    body { margin: 25px; }
                                }

                                /* La page fait la hauteur d'une feuille A4 (297mm - 2 x 18mm de marge)
                                   pour pouvoir coller les signatures en bas. */
                                .page {
                                    display: flex;
                                    flex-direction: column;
                                    min-height: 255mm;
                                }

                                .contenu-page {
                                    flex: 1 0 auto;
                                }

                                .zone-signatures {
                                    margin-top: auto;
                                    padding-top: 30px;
                                    page-break-inside: avoid;
                                }

                                .header-officiel {
                                    margin-bottom: 20px;
                                }

                                .republique {
                                    text-align: center;
                                    font-size: 24px;
                                    font-weight: bold;
                                    text-decoration: underline;
                                    margin-bottom: 15px;
                                    direction: rtl;
                                }

                                .ministere {
                                    text-align: right;
                                    direction: rtl;
                                    font-size: 16px;
                                    font-weight: bold;
                                    line-height: 1.8;
                                    margin-bottom: 15px;
                                }

                                .ref-box {
                                    text-align: left;
                                    direction: ltr;
                                    font-size: 13px;
                                    font-weight: bold;
                                    margin-top: 8px;
                                }

                                .title-container {
                                    text-align: center;
                                    margin: 25px 0 20px 0;
                                }

                                .title-box {
                                    display: inline-block;
                                    border: 1.5px solid #000;
                                    padding: 5px 35px;
                                    font-size: 22px;
                                    font-weight: bold;
                                }

                                .info-section {
                                    font-size: 14px;
                                    line-height: 1.8;
                                    margin-bottom: 15px;
                                    font-weight: bold;
                                    direction: rtl;
                                    text-align: right;
                                }

                                .info-row {
                                    margin-bottom: 4px;
                                }

                                .contenu-text {
                                    font-size: 14px;
                                    font-weight: normal;
                                    margin: 15px 0 20px 0;
                                    text-align: justify;
                                    line-height: 1.8;
                                    direction: rtl;
                                }

                                .section-title {
                                    font-size: 16px;
                                    font-weight: bold;
                                    margin-top: 20px;
                                    margin-bottom: 10px;
                                    text-decoration: underline;
                                    direction: rtl;
                                    text-align: right;
                                }

                                /*
                                   Pas de tableau et pas de cadre.
                                   La ligne commence à gauche par un tiret.
                                */
                                .mouvement-line {
                                    border: none;
                                    outline: none;
                                    box-shadow: none;

                                    display: block;
                                    width: 100%;

                                    margin: 0 0 8px 0;
                                    padding: 0;

                                    direction: ltr;
                                    text-align: left;

                                    font-family: Arial, 'Times New Roman', sans-serif;
                                    font-size: 13px;
                                    line-height: 1.8;

                                    white-space: normal;
                                    page-break-inside: avoid;
                                }

                                .separateur {
                                    padding-left: 4px;
                                    padding-right: 4px;
                                }

                                .obs-section {
                                    font-size: 14px;
                                    margin-top: 18px;
                                    font-weight: bold;
                                    line-height: 1.8;
                                    direction: rtl;
                                    text-align: right;
                                }

                                .signatures-table {
                                    width: 100%;
                                    border: none;
                                    margin-top: 0;
                                    border-collapse: collapse;
                                }

                                .signatures-table td {
                                    border: none;
                                    font-size: 14px;
                                    font-weight: bold;
                                    text-align: center;
                                    width: 50%;
                                    vertical-align: top;
                                    height: 80px;
                                }

                                @media print {
                                    .no-print {
                                        display: none;
                                    }
                                }

                            </style>
                            </head>
                            <body>
                            <div class='page'>
                            <div class='contenu-page'>
                            ");

            // ============================================================
            // 4. EN-TÊTE OFFICIEL
            // ============================================================

            html.Append(@"
                        <div class='header-officiel'>

                            <div class='republique'>
                                الجمهورية الجزائرية الديمقراطية الشعبية
                            </div>

                            <div class='ministere'>
                                <div>وزارة الداخليـــــة و الجماعات المحلية.</div>
                                <div>ولايــــة غليزان</div>
                                <div>مديرية المواصلات السلكية و اللاسلكية الوطنية</div>
                                <div>مصلحة الادارة و الامداد / مكتب الوسائل العامة و المخزن.</div>
                            </div>
                        ");

            if (!string.IsNullOrWhiteSpace(refMouvement))
            {
                html.Append(
                    "<div class='ref-box'>" +
                    "Réf : " +
                    WebUtility.HtmlEncode(refMouvement) +
                    "</div>"
                );
            }

            html.Append("</div>");

            // ============================================================
            // 5. TITRE DU DOCUMENT
            // ============================================================

            html.Append("<div class='title-container'>");

            html.Append(
                "<span class='title-box'>- " +
                WebUtility.HtmlEncode(nomMouvement) +
                " -</span>"
            );

            html.Append("</div>");

            // ============================================================
            // 6. INFORMATIONS DE L'EMPLOYÉ
            // ============================================================

            html.Append("<div class='info-section'>");

            if (!string.IsNullOrWhiteSpace(nomEmploye))
            {
                html.Append(
                    "<div class='info-row'>" +
                    "انا الممضي اسفله : " +
                    WebUtility.HtmlEncode(nomEmploye) +
                    "</div>"
                );
            }

            if (!string.IsNullOrWhiteSpace(fonctionEmploye) ||
                !string.IsNullOrWhiteSpace(deptEmploye))
            {
                string fonctionDepartement = "";

                if (!string.IsNullOrWhiteSpace(fonctionEmploye))
                {
                    fonctionDepartement = fonctionEmploye;
                }

                if (!string.IsNullOrWhiteSpace(deptEmploye))
                {
                    if (!string.IsNullOrWhiteSpace(fonctionDepartement))
                    {
                        fonctionDepartement += " / ";
                    }

                    fonctionDepartement += deptEmploye;
                }

                html.Append(
                    "<div class='info-row'>" +
                    "الوظيفة : " +
                    WebUtility.HtmlEncode(fonctionDepartement) +
                    "</div>"
                );
            }

            if (!string.IsNullOrWhiteSpace(dateMouvement))
            {
                html.Append(
                    "<div class='info-row'>" +
                    "بتاريخ : " +
                    WebUtility.HtmlEncode(dateMouvement) +
                    "</div>"
                );
            }

            html.Append("</div>");

            // ============================================================
            // 7. CONTENU EXPLICATIF
            // ============================================================

            if (!string.IsNullOrWhiteSpace(contenuMouvement))
            {
                html.Append(
                    "<div class='contenu-text'>" +
                    WebUtility.HtmlEncode(contenuMouvement) +
                    "</div>"
                );
            }

            // ============================================================
            // 8. FONCTIONS D'AFFICHAGE
            // ============================================================

            string Encoder(string valeur)
            {
                if (string.IsNullOrWhiteSpace(valeur))
                {
                    return "";
                }

                return WebUtility.HtmlEncode(valeur.Trim());
            }

            void AjouterValeurSansTitre(
                StringBuilder ligne,
                string valeur,
                ref bool premier)
            {
                if (string.IsNullOrWhiteSpace(valeur))
                {
                    return;
                }

                if (!premier)
                {
                    ligne.Append(
                        "<span class='separateur'>, </span>"
                    );
                }

                ligne.Append(
                    Encoder(valeur)
                );

                premier = false;
            }

            void AjouterValeurAvecTitre(
                StringBuilder ligne,
                string titre,
                string valeur,
                ref bool premier)
            {
                if (string.IsNullOrWhiteSpace(valeur))
                {
                    return;
                }

                if (!premier)
                {
                    ligne.Append(
                        "<span class='separateur'>, </span>"
                    );
                }

                ligne.Append(
                    WebUtility.HtmlEncode(titre) +
                    ":" +
                    Encoder(valeur)
                );

                premier = false;
            }

            void GenererLignesMateriel(
                List<Dictionary<string, string>> items,
                string titreSection)
            {
                if (items == null || items.Count == 0)
                {
                    return;
                }

                html.Append(
                    "<div class='section-title'>" +
                    WebUtility.HtmlEncode(titreSection) +
                    "</div>"
                );

                foreach (var item in items)
                {
                    var ligne = new StringBuilder();

                    bool premier = true;

                    // Début obligatoire de chaque ligne
                    ligne.Append("- ");

                    // Quantité : en lettres + chiffres, sans titre "QTE"
                    // Exemple : Douze (12)
                    string qteTexte = FormaterQuantiteEnLettres(item["quantite"]);

                    if (!string.IsNullOrEmpty(qteTexte))
                    {
                        ligne.Append(Encoder(qteTexte));
                        premier = false;
                    }

                    // Ces colonnes sont affichées sans leurs titres
                    AjouterValeurSansTitre(
                        ligne,
                        item["famille_nom"],
                        ref premier
                    );

                    AjouterValeurSansTitre(
                        ligne,
                        item["marque_nom"],
                        ref premier
                    );

                    AjouterValeurSansTitre(
                        ligne,
                        item["designation_modele"],
                        ref premier
                    );

                    // Référence : masquée si elle est automatique (AUTO-xxxxx),
                    // affichée dans tous les autres cas
                    if (!EstReferenceAutomatique(item["reference_modele"]))
                    {
                        AjouterValeurSansTitre(
                            ligne,
                            item["reference_modele"],
                            ref premier
                        );
                    }

                    // État conserve son titre
                    AjouterValeurAvecTitre(
                        ligne,
                        "État",
                        item["etat_a_la_mouvement"],
                        ref premier
                    );

                    // Observation est affichée sans son titre
                    AjouterValeurSansTitre(
                        ligne,
                        item["obs_ligne"],
                        ref premier
                    );

                    // Affichage uniquement si au moins un champ existe
                    if (!premier)
                    {
                        html.Append(
                            "<div class='mouvement-line'>" +
                            ligne.ToString() +
                            "</div>"
                        );
                    }
                }
            }

            // ============================================================
            // 9. AFFICHAGE DES MATÉRIELS
            // ============================================================

            GenererLignesMateriel(
                lignesSortie,
                "العتاد المأخوذ :"
            );

            GenererLignesMateriel(
                lignesEntree,
                "العتاد المرجع :"
            );

            // ============================================================
            // 10. OBSERVATION GLOBALE
            // ============================================================

            if (!string.IsNullOrWhiteSpace(obsMouvement))
            {
                html.Append(
                    "<div class='obs-section'>" +
                    "ملاحظة : " +
                    WebUtility.HtmlEncode(obsMouvement) +
                    "</div>"
                );
            }

            // ============================================================
            // 11. SIGNATURES (toujours en bas de la page)
            // ============================================================

            html.Append(@"
                            </div>
                            <div class='zone-signatures'>
                            <table class='signatures-table'>
                                <tr>

                                    <td>
                                        إمضاء المستلم(ة)
                                    </td>

                                    <td>
                                        إمضاء المكلف(ة) بمكتب الوسائل العامة و المخزن 
                                    </td>
                                </tr>
                            </table>
                            </div>
                            </div>
                            ");

            html.Append(@"
                            </body>
                            </html>
                            ");

            // ============================================================
            // 12. SAUVEGARDE ET OUVERTURE
            // ============================================================

            string tempFile = Path.Combine(
                Path.GetTempPath(),
                $"bon_mouvement_{mouvementId}_{DateTime.Now:yyyyMMdd_HHmmss}.html"
            );

            File.WriteAllText(
                tempFile,
                html.ToString(),
                Encoding.UTF8
            );

            Process.Start(
                new ProcessStartInfo(tempFile)
                {
                    UseShellExecute = true
                }
            );
        }

        private void PeuplerListeFiltrageMVM()
        {
            if (listeDeFiltrageMVMComboBox.Items.Count > 0) return;
            listeDeFiltrageMVMComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            foreach (var (affichage, _) in ColonnesFiltrablesMVM)
                listeDeFiltrageMVMComboBox.Items.Add(affichage);
            listeDeFiltrageMVMComboBox.SelectedIndex = 0;
        }

        private void AppliquerFiltreMVM()
        {
            if (tableMVMDataGridView.DataSource is not DataTable dt) return;

            string recherche = filtreTableMVMTextBox.Text.Trim().Replace("'", "''");
            DataView vue = dt.DefaultView;

            if (string.IsNullOrEmpty(recherche))
            {
                vue.RowFilter = "";
                return;
            }

            int index = listeDeFiltrageMVMComboBox.SelectedIndex;
            string colonne = (index >= 0 && index < ColonnesFiltrablesMVM.Length)
                ? ColonnesFiltrablesMVM[index].Colonne : "";

            if (string.IsNullOrEmpty(colonne))
            {
                var conds = new List<string>();
                foreach (DataColumn col in dt.Columns)
                    conds.Add($"CONVERT([{col.ColumnName}], 'System.String') LIKE '%{recherche}%'");
                vue.RowFilter = string.Join(" OR ", conds);
            }
            else
            {
                vue.RowFilter = $"CONVERT([{colonne}], 'System.String') LIKE '%{recherche}%'";
            }
        }

        private void filtreTableMVMTextBox_TextChanged(object sender, EventArgs e) => AppliquerFiltreMVM();
        private void listeDeFiltrageMVMComboBox_SelectedIndexChanged(object sender, EventArgs e) => AppliquerFiltreMVM();

        private void btnNouveauMouvement_Click(object sender, EventArgs e)
        {
            using (var frm = new FrmAjouterMouvement(this))
            {
                if (frm.ShowDialog(this) == DialogResult.OK)
                {
                    ChargerMouvements();
                    RafraichirAccueil();
                }
            }
        }

        // Recalcule les cartes, l'alerte de quantité, le graphique et les employés du tableau de bord
        // s'il est actuellement affiché (le stock a changé suite à un mouvement).
        private void RafraichirAccueil()
        {
            if (home_container.Tag is FrmAccueil accueilOuvert && !accueilOuvert.IsDisposed)
                accueilOuvert.RafraichirDashboard();
        }



        // =====================================================
        // SECTION 5 : INVENTAIRES
        // =====================================================

        // =====================================================
        // SECTION 5 : INVENTAIRES
        // =====================================================

        public void ChargerInventaires()
        {
            string sql = @"
                SELECT 
                    i.id AS 'ID',
                    CASE WHEN i.type_inventaire = 'Annuel' THEN '—' ELSE i.structure END AS 'Structure',
                    CASE WHEN i.type_inventaire = 'Annuel' THEN '—' ELSE i.bureau END AS 'Bureau',
                    i.type_inventaire AS 'Type',
                    i.date_inventaire AS 'Date'
                FROM Inventaire i
                ORDER BY i.id DESC";

            DataTable dt = DatabaseHelper.ExecuteQuery(sql);

            // 1. Réinitialiser proprement la table (comme dans Mouvements)
            tableINVDataGridView.DataSource = null;
            tableINVDataGridView.Columns.Clear();

            // 2. Assigner la nouvelle source de données
            tableINVDataGridView.AutoGenerateColumns = true;
            tableINVDataGridView.DataSource = dt;

            // 3. Ajouter les actions et le filtre
            AjouterColonnesActionsINV();
            PeuplerListeFiltrageINV();
            AppliquerFiltreINV();
        }
        private void AjouterColonnesActionsINV()
        {
            string[] colsActions = { "colModifierINV", "colSupprimerINV", "colImprimerINV" };
            foreach (var colName in colsActions)
            {
                if (tableINVDataGridView.Columns.Contains(colName))
                    tableINVDataGridView.Columns.Remove(colName);
            }

            tableINVDataGridView.Columns.Add(new DataGridViewButtonColumn
            {
                Name = "colModifierINV",
                HeaderText = "Modifier",
                Width = 60,
                FlatStyle = FlatStyle.Flat
            });

            tableINVDataGridView.Columns.Add(new DataGridViewButtonColumn
            {
                Name = "colSupprimerINV",
                HeaderText = "Supprimer",
                Width = 60,
                FlatStyle = FlatStyle.Flat
            });

            tableINVDataGridView.Columns.Add(new DataGridViewButtonColumn
            {
                Name = "colImprimerINV",
                HeaderText = "Imprimer",
                Width = 60,
                FlatStyle = FlatStyle.Flat
            });
        }

        private void TableINVDataGridView_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0) return;

            Point mousePos = tableINVDataGridView.PointToClient(Cursor.Position);
            bool isHovered = e.CellBounds.Contains(mousePos);
            bool isClicked = isHovered && (Control.MouseButtons == MouseButtons.Left);
            int iconSize = 18;

            if (e.ColumnIndex == tableINVDataGridView.Columns["colModifierINV"]?.Index)
            {
                DessinerBoutonAction(e, isHovered, isClicked,
                    Color.FromArgb(240, 253, 244), Color.FromArgb(220, 252, 231), Color.FromArgb(187, 247, 208),
                    Color.FromArgb(134, 239, 172), "pencil_icon.png", iconSize);
            }
            else if (e.ColumnIndex == tableINVDataGridView.Columns["colSupprimerINV"]?.Index)
            {
                DessinerBoutonAction(e, isHovered, isClicked,
                    Color.FromArgb(254, 242, 242), Color.FromArgb(254, 226, 226), Color.FromArgb(254, 202, 202),
                    Color.FromArgb(252, 165, 165), "delet_icon.png", iconSize);
            }
            else if (e.ColumnIndex == tableINVDataGridView.Columns["colImprimerINV"]?.Index)
            {
                DessinerBoutonAction(e, isHovered, isClicked,
                    Color.FromArgb(239, 246, 255), Color.FromArgb(219, 234, 254), Color.FromArgb(191, 219, 254),
                    Color.FromArgb(147, 197, 253), "imprimerbleu.png", iconSize);
            }
        }

        private void tableINVDataGridView_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            var grid = (DataGridView)sender;
            string colName = grid.Columns[e.ColumnIndex].Name;
            int inventaireId = Convert.ToInt32(grid.Rows[e.RowIndex].Cells["ID"].Value);

            if (colName == "colModifierINV")
            {
                // Le type (Bureau / Annuel) est relu depuis la base par le formulaire
                using (var frm = new FrmAjouterInventaire(this, inventaireId))
                {
                    if (frm.ShowDialog(this) == DialogResult.OK)
                        ChargerInventaires();
                }
            }
            else if (colName == "colSupprimerINV")
            {
                string typeInv = grid.Rows[e.RowIndex].Cells["Type"].Value?.ToString() ?? "Bureau";
                string bureau = grid.Rows[e.RowIndex].Cells["Bureau"].Value?.ToString() ?? "";
                string cible = typeInv == "Annuel" ? "l'inventaire annuel" : $"la fiche d'inventaire du bureau '{bureau}'";
                var confirm = MessageBox.Show(
                    $"Voulez-vous vraiment supprimer {cible} ?\nCette action supprimera également toutes ses lignes.",
                    "Confirmer la suppression", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                if (confirm == DialogResult.Yes)
                {
                    try
                    {
                        // Ligne_inventaire a ON DELETE CASCADE sur inventaire_id :
                        // la suppression de l'Inventaire suffit, les lignes suivent automatiquement.
                        DatabaseHelper.ExecuteNonQuery("DELETE FROM Inventaire WHERE id = @id", new SqliteParameter("@id", inventaireId));
                        ChargerInventaires();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Erreur lors de la suppression : {ex.Message}",
                            "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            else if (colName == "colImprimerINV")
            {
                ImprimerFicheInventaire(inventaireId, grid.Rows[e.RowIndex]);
            }
        }

        // Impression au format EXACT de la fiche papier :
        // FICHE D'INVENTAIRE / STRUCTURE / BUREAU / DATE / N°-DESIGNATION-QTE-OBSERVATION
        private void ImprimerFicheInventaire(int inventaireId, DataGridViewRow row)
        {
            string structure = row.Cells["Structure"].Value?.ToString() ?? "";
            string bureau = row.Cells["Bureau"].Value?.ToString() ?? "";
            string date = row.Cells["Date"].Value?.ToString() ?? "";
            bool annuel = (row.Cells["Type"].Value?.ToString() ?? "Bureau") == "Annuel";
            if (annuel) { structure = ""; bureau = ""; }   // affichés '—' dans la grille

            // MIGRATION : Ligne_inventaire référence directement modele_id désormais.
            string sqlLignes = @"
                SELECT li.quantite, li.observation, m.designation AS modele
                FROM Ligne_inventaire li
                JOIN Modele m ON li.modele_id = m.id
                WHERE li.inventaire_id = @id
                ORDER BY li.id";

            DataTable lignes = DatabaseHelper.ExecuteQuery(sqlLignes, new SqliteParameter("@id", inventaireId));

            var html = new StringBuilder();
            html.Append("<html><head><meta charset='utf-8'><style>");
            html.Append("body{font-family:times new roman, sans-serif; margin:35px; color:#000;}");
            html.Append(".header-officiel{text-align:right; direction:rtl; font-size:13px; font-weight:bold; line-height:1.6; margin-bottom:15px;}");
            html.Append(".republique{text-align:center; font-size:24px; font-weight:bold; text-decoration:underline; margin-bottom:15px; direction:rtl;}");
            html.Append(".entet{text-align:right; font-size:16px; font-weight:bold;margin-bottom:15px; direction:rtl;}");
            html.Append(".infos{font-size:14px; font-weight:bold; margin-bottom:5px;}");
            html.Append(".date{text-align:right; font-size:14px; font-weight:bold; margin-bottom:15px;}");
            html.Append(".titre-box{text-align:center; margin:25px 0;}");
            html.Append(".titre-box span{display:inline-block; border:2px solid #000; padding:6px 40px; font-size:20px; font-weight:bold; letter-spacing:1px;}");
            html.Append("table{border-collapse:collapse; width:100%; margin-top:20px;}");
            html.Append("th,td{border:1px solid #000; padding:8px 10px; font-size:13px;}");
            html.Append("th{background:#f0f2f5; text-align:center;}");
            html.Append("td.num{text-align:center; width:6%;}");
            html.Append("td.qte{text-align:center; width:8%;}");
            html.Append(".signature{margin-top:60px; text-align:center; font-size:13px; font-weight:bold;}");
            html.Append("@media print{.no-print{display:none;}}");
            html.Append("</style></head><body>");

            html.Append("<div class='republique'>الجمهورية الجزائرية الديمقراطية الشعبية</div>");
            html.Append("    <div class='entet'>");
            html.Append("      <div>وزارة الداخليـــــة و الجماعات المحلية .</div>");
            html.Append("      <div>ولايــــة غليزان </div>");
            html.Append("      <div>مديرية المواصلات السلكية و اللاسلكية الوطنية</div>");
            html.Append("      <div>مصلحة الادارة و الامداد / مكتب الوسائل العامة و المخزن.</div>");
            html.Append("    </div>");
            if (!annuel)
            {
                html.Append("<div class='infos'>STRUCTURE : " + WebUtility.HtmlEncode(structure) + "</div>");
                html.Append("<div class='infos'>BUREAU : " + WebUtility.HtmlEncode(bureau) + "</div>");
            }
            html.Append("<div class='date'>" + WebUtility.HtmlEncode(date) + "</div>");

            html.Append("<div class='titre-box'><span>" + (annuel ? "FICHE D'INVENTAIRE ANNUEL" : "FICHE D'INVENTAIRE") + "</span></div>");

            html.Append("<table><tr><th style='width:6%;'>N°</th><th>DESIGNATION</th><th style='width:8%;'>QTE</th><th>OBSERVATION</th></tr>");

            int n = 1;
            foreach (DataRow r in lignes.Rows)
            {
                html.Append("<tr>");
                html.Append($"<td class='num'>{n:D2}</td>");
                html.Append($"<td>-{WebUtility.HtmlEncode(r["modele"]?.ToString() ?? "")}</td>");
                html.Append($"<td class='qte'>{Convert.ToInt32(r["quantite"]):D2}</td>");
                html.Append($"<td>{WebUtility.HtmlEncode(r["observation"]?.ToString() ?? "")}</td>");
                html.Append("</tr>");
                n++;
            }
            html.Append("</table>");

            html.Append("<div class='signature'>LE RESPONSABLE DU BUREAU</div>");

            html.Append("</body></html>");

            string tempFile = Path.Combine(Path.GetTempPath(), $"fiche_inventaire_{inventaireId}_{DateTime.Now:yyyyMMdd_HHmmss}.html");
            File.WriteAllText(tempFile, html.ToString(), Encoding.UTF8);
            Process.Start(new ProcessStartInfo(tempFile) { UseShellExecute = true });
        }

        private void PeuplerListeFiltrageINV()
        {
            listeDeFiltrageINVComboBox.Items.Clear(); // Vider avant de peupler
            listeDeFiltrageINVComboBox.DropDownStyle = ComboBoxStyle.DropDownList;

            foreach (var (affichage, _) in ColonnesFiltrablesINV)
                listeDeFiltrageINVComboBox.Items.Add(affichage);

            listeDeFiltrageINVComboBox.SelectedIndex = 0;
        }
        private void AppliquerFiltreINV()
        {
            if (tableINVDataGridView.DataSource is not DataTable dt) return;

            string recherche = filtreTableINVTextBox.Text.Trim().Replace("'", "''");
            DataView vue = dt.DefaultView;

            if (string.IsNullOrEmpty(recherche))
            {
                vue.RowFilter = "";
                return;
            }

            int index = listeDeFiltrageINVComboBox.SelectedIndex;
            string colonne = (index >= 0 && index < ColonnesFiltrablesINV.Length)
                ? ColonnesFiltrablesINV[index].Colonne : "";

            if (string.IsNullOrEmpty(colonne))
            {
                var conditions = new List<string>();
                foreach (DataColumn col in dt.Columns)
                    conditions.Add($"CONVERT([{col.ColumnName}], 'System.String') LIKE '%{recherche}%'");
                vue.RowFilter = string.Join(" OR ", conditions);
            }
            else
            {
                vue.RowFilter = $"CONVERT([{colonne}], 'System.String') LIKE '%{recherche}%'";
            }
        }

        private void filtreTableINVTextBox_TextChanged(object sender, EventArgs e) => AppliquerFiltreINV();
        private void listeDeFiltrageINVComboBox_SelectedIndexChanged(object sender, EventArgs e) => AppliquerFiltreINV();

        // Inventaire de bureau (même formulaire, type = Bureau)
        private void btnNouveauInventaire_Click(object sender, EventArgs e)
            => OuvrirNouvelInventaire(TypeInventaire.Bureau);

        // Inventaire annuel (même formulaire, type = Annuel : lignes chargées depuis le stock)
        private void btnNouvInvAnne_Click(object sender, EventArgs e)
            => OuvrirNouvelInventaire(TypeInventaire.Annuel);

        private void OuvrirNouvelInventaire(TypeInventaire type)
        {
            using (var frm = new FrmAjouterInventaire(this, null, type))
            {
                if (frm.ShowDialog(this) == DialogResult.OK && frm.InventaireEnregistre)
                    ChargerInventaires();
            }
        }



        //******************************************************************
        #region Authentification & Gestion utilisateurs
        //******************************************************************

        // Affiche le nom de l'utilisateur connecté dans le Label
        // "lblUtilisateurConnecte" (à créer dans le Designer si absent).
        private void AfficherInfoUtilisateur()
        {
            if (lblUtilisateurConnecte != null)
                lblUtilisateurConnecte.Text = SessionUtilisateur.NomAffichage;
        }

        // Bouton "Gérer utilisateurs" (à créer dans le Designer, nommé
        // btnGererUtilisateurs — à placer dans le menu latéral ou en haut).
        private void btnGererUtilisateurs_Click(object sender, EventArgs e)
        {
            using (var frm = new FrmGererUtilisateurs())
                frm.ShowDialog(this);
        }

        // Bouton "Déconnexion" (nommé btnDeconnexion dans le Designer).
        // Ferme Form1, lance un nouveau FrmLogin, et rouvre Form1 si succès.
        private void btnDeconnexion_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(
                    "Voulez-vous vous déconnecter ?",
                    "Déconnexion",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            SessionUtilisateur.Deconnecter();
            Hide();

            using (var login = new FrmLogin())
            {
                if (login.ShowDialog() == DialogResult.OK && SessionUtilisateur.EstConnecte)
                {
                    AfficherInfoUtilisateur();
                    Show();
                }
                else
                {
                    // L'utilisateur a fermé le login sans se reconnecter -> on quitte
                    Application.Exit();
                }
            }
        }

        // Bouton "Changer mon mot de passe" (nommé btnChangerMdp).
        // Ouvre directement la fiche de modification pour le compte connecté.
        private void btnChangerMdp_Click(object sender, EventArgs e)
        {
            using (var frm = new FrmAjouterUtilisateur(SessionUtilisateur.Id))
            {
                if (frm.ShowDialog(this) == DialogResult.OK)
                    MessageBox.Show("Mot de passe mis à jour avec succès.",
                        "Succès", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        #endregion


        private void ChargerFormulaireModeleDansTab()
        {
            // 1. Si le formulaire n'existe pas ou a été détruit, on l'instancie
            if (_frmModeleEmbed == null || _frmModeleEmbed.IsDisposed)
            {
                Modèlles.Controls.Clear();

                _frmModeleEmbed = new FrmGererModeles(this)
                {
                    TopLevel = false,
                    FormBorderStyle = FormBorderStyle.None,
                    Dock = DockStyle.Fill
                };

                Modèlles.Controls.Add(_frmModeleEmbed);
                _frmModeleEmbed.Show();
            }
            else
            {
                // 2. Si le formulaire existe déjà, on rafraîchit la liste
                _frmModeleEmbed.ChargerListe();
            }
        }

        //******************************************************************
        //********** Afficher la gestion des catigories dans le TabPage *******
        //******************************************************************
        private void ChargerFormulaireCategorieDansTab()
        {
            if (_frmCategorieEmbed == null || _frmCategorieEmbed.IsDisposed)
            {
                Categories.Controls.Clear();

                _frmCategorieEmbed = new FrmGererCategories(this)
                {
                    TopLevel = false,
                    FormBorderStyle = FormBorderStyle.None,
                    Dock = DockStyle.Fill
                };

                Categories.Controls.Add(_frmCategorieEmbed);
                _frmCategorieEmbed.Show();
            }
            else
            {
                _frmCategorieEmbed.ChargerListe();
            }
        }

        //******************************************************************
        //********** Afficher la gestion des marques dans le TabPage *******
        //******************************************************************
        private void ChargerFormulaireMarqueDansTab()
        {
            if (_frmMarqueEmbed == null || _frmMarqueEmbed.IsDisposed)
            {
                Marques.Controls.Clear(); // Remplacez "Marques" par le nom exact de votre TabPage dans le Designer

                _frmMarqueEmbed = new FrmGererMarques(this)
                {
                    TopLevel = false,
                    FormBorderStyle = FormBorderStyle.None,
                    Dock = DockStyle.Fill
                };

                Marques.Controls.Add(_frmMarqueEmbed);
                _frmMarqueEmbed.Show();
            }
            else
            {
                _frmMarqueEmbed.ChargerListe();
            }
        }

        // 3. Événement mis à jour pour gérer la navigation entre tous les onglets
        private void stock_containers_SelectedIndexChanged(object sender, EventArgs e)
        {
            ChargerOngletActifStock();
        }

       
    }
}