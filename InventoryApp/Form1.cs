using InventoryApp.Data;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Text;
using System.Windows.Forms;
using static System.Runtime.InteropServices.JavaScript.JSType;


namespace InventoryApp
{
    public partial class Form1 : Form
    {

        //les formulaires embarqués pour les onglets de tabpages
        private FrmGererModeles _frmModeleEmbed;
        private FrmGererCategories _frmCategorieEmbed;
        private FrmGererMarques _frmMarqueEmbed;


        // Colonnes filtrables centralisées
        private static readonly (string Affichage, string Colonne)[] ColonnesFiltrablesEquipement = new[]
        {
            ("Tous les champs", ""),
            ("Modèle",           "Modèle"),
            ("N° Série",         "N° Série"),
            ("Référence Modèle", "Référence Modèle"),
            ("Marque",           "Marque"),
            ("Catégorie",        "Catégorie"),
            ("Utilisé par",      "Utilisé par"),
            ("Statut",           "Statut"),
            ("Code-Barre",       "Code-Barre"),
            ("Date Acquisition", "Date Acquisition"),
            ("Observations",     "Observations"),
            ("Emplacement",      "Emplacement")
        };

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

            // Configuration DataGridView Equipements
            table_equipements.RowTemplate.Height = 38;
            table_equipements.CellPainting += Table_equipements_CellPainting;
            table_equipements.CellContentClick += table_equipements_CellContentClick;
            table_equipements.CellMouseMove += (s, e) => table_equipements.InvalidateCell(e.ColumnIndex, e.RowIndex);
            table_equipements.CellMouseLeave += (s, e) => table_equipements.InvalidateCell(e.ColumnIndex, e.RowIndex);

            // Configuration DataGridView Mouvements
            tableMVMDataGridView.RowTemplate.Height = 38;
            tableMVMDataGridView.CellPainting += TableMVMDataGridView_CellPainting;
            tableMVMDataGridView.CellClick += tableMVMDataGridView_CellClick;
            tableMVMDataGridView.CellMouseMove += (s, e) => tableMVMDataGridView.InvalidateCell(e.ColumnIndex, e.RowIndex);
            tableMVMDataGridView.CellMouseLeave += (s, e) => tableMVMDataGridView.InvalidateCell(e.ColumnIndex, e.RowIndex);

            // Chargements initiaux
            ChargerEquipements();
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


            stock_containers.SelectedIndexChanged += stock_containers_SelectedIndexChanged;
        }

        // =====================================================
        // SECTION 1 : ÉQUIPEMENTS & STOCK
        // =====================================================

        public void ChargerEquipements()
        {
            string sql = @"
                SELECT 
                    e.id AS 'ID',
                    c.designation AS 'Catégorie',
                    mq.designation AS 'Marque',
                    m.designation AS 'Modèle',
                    m.reference AS 'Référence Modèle',
                    e.numero_serie AS 'N° Série',
                    e.etat AS 'État',
                    e.statut AS 'Statut',
                    COALESCE((
                        SELECT GROUP_CONCAT(emp_info, ' | ')
                        FROM (
                            SELECT emp.nom || ' ' || emp.prenom || ' (' || COALESCE(emp.departement, 'Sans Service') || ')' AS emp_info
                            FROM Ligne_mouvement lm
                            JOIN Mouvement mvt ON lm.mouvement_id = mvt.id
                            JOIN Employe emp ON mvt.employe_id = emp.id
                            WHERE lm.equipement_id = e.id
                            ORDER BY mvt.date_mouvement DESC, mvt.id DESC
                            LIMIT 3
                        )
                    ), '') AS 'Utilisé par',
                    e.code_barre AS 'Code-Barre',
                    e.date_acquisition AS 'Date Acquisition',
                    e.observations AS 'Observations',
                    e.emplacement AS 'Emplacement'
                FROM Equipement e
                JOIN Modele m ON e.modele_id = m.id
                LEFT JOIN Marque mq ON m.marque_id = mq.id
                LEFT JOIN Categorie c ON m.categorie_id = c.id
                ORDER BY e.id DESC";

            DataTable dt = DatabaseHelper.ExecuteQuery(sql);

            // 1. Réinitialiser la source et vider les colonnes préexistantes
            table_equipements.DataSource = null;
            table_equipements.Columns.Clear();

            // 2. Générer automatiquement les colonnes texte à partir du DataTable
            table_equipements.AutoGenerateColumns = true;
            table_equipements.DataSource = dt;

            if (table_equipements.Columns.Contains("Utilisé par"))
                table_equipements.Columns["Utilisé par"].ValueType = typeof(string);

            // 3. Ajouter proprement les boutons d'action
            AjouterColonnesActions();
            PeuplerListeFiltrage();
            AppliquerFiltre();
        }
        private void AjouterColonnesActions()
        {
            string[] colsActions = { "colModifier", "colSupprimer", "colImprimer" };
            foreach (var colName in colsActions)
            {
                if (table_equipements.Columns.Contains(colName))
                    table_equipements.Columns.Remove(colName);
            }

            table_equipements.Columns.Add(new DataGridViewButtonColumn
            {
                Name = "colModifier",
                HeaderText = "Modifier",
                Width = 60,
                FlatStyle = FlatStyle.Flat
            });

            table_equipements.Columns.Add(new DataGridViewButtonColumn
            {
                Name = "colSupprimer",
                HeaderText = "Supprimer",
                Width = 60,
                FlatStyle = FlatStyle.Flat
            });

            table_equipements.Columns.Add(new DataGridViewButtonColumn
            {
                Name = "colImprimer",
                HeaderText = "Imprimer",
                Width = 60,
                FlatStyle = FlatStyle.Flat
            });
        }
        private void Table_equipements_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0) return;

            Point mousePos = table_equipements.PointToClient(Cursor.Position);
            bool isHovered = e.CellBounds.Contains(mousePos);
            bool isClicked = isHovered && (Control.MouseButtons == MouseButtons.Left);
            int iconSize = 18;

            if (e.ColumnIndex == table_equipements.Columns["colModifier"]?.Index)
            {
                DessinerBoutonAction(e, isHovered, isClicked,
                    Color.FromArgb(240, 253, 244), Color.FromArgb(220, 252, 231), Color.FromArgb(187, 247, 208),
                    Color.FromArgb(134, 239, 172), "pencil_icon.png", iconSize);
            }
            else if (e.ColumnIndex == table_equipements.Columns["colSupprimer"]?.Index)
            {
                DessinerBoutonAction(e, isHovered, isClicked,
                    Color.FromArgb(254, 242, 242), Color.FromArgb(254, 226, 226), Color.FromArgb(254, 202, 202),
                    Color.FromArgb(252, 165, 165), "delet_icon.png", iconSize);
            }
            else if (e.ColumnIndex == table_equipements.Columns["colImprimer"]?.Index)
            {
                DessinerBoutonAction(e, isHovered, isClicked,
                    Color.FromArgb(239, 246, 255), // Fond normal (Bleu très clair)
                    Color.FromArgb(219, 234, 254), // Fond au survol / Hover (Bleu doux)
                    Color.FromArgb(191, 219, 254), // Fond au clic / Click (Bleu plus soutenu)
                    Color.FromArgb(147, 197, 253), // Bordure (Bleu moyen)
                    "imprimerbleu.png",
                    iconSize);
            }
        }

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

        private void table_equipements_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            var grid = (DataGridView)sender;
            string colName = grid.Columns[e.ColumnIndex].Name;
            int equipementId = Convert.ToInt32(grid.Rows[e.RowIndex].Cells["ID"].Value);

            if (colName == "colModifier")
            {
                using (var frm = new FrmAjouterArticle(this, equipementId))
                {
                    if (frm.ShowDialog(this) == DialogResult.OK)
                        ChargerEquipements();
                }
            }
            else if (colName == "colSupprimer")
            {
                string modele = grid.Rows[e.RowIndex].Cells["Modèle"].Value?.ToString() ?? "";
                var confirm = MessageBox.Show(
                    $"Voulez-vous vraiment supprimer l'équipement #{equipementId} ({modele}) ?\nCette action est irréversible.",
                    "Confirmer la suppression", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                if (confirm == DialogResult.Yes)
                {
                    try
                    {
                        DatabaseHelper.ExecuteNonQuery("DELETE FROM Equipement WHERE id = @id", new SqliteParameter("@id", equipementId));
                        ChargerEquipements();
                    }
                    catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
                    {
                        MessageBox.Show("Impossible de supprimer : cet équipement est référencé dans un historique.\nMarquez-le comme 'Réformé' à la place.",
                            "Suppression refusée", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            else if (colName == "colImprimer")
            {
                ImprimerFicheEquipement(grid.Rows[e.RowIndex]);
            }
        }

        private void ImprimerFicheEquipement(DataGridViewRow row)
        {
            var html = new StringBuilder();
            html.Append("<html><head><meta charset='utf-8'><style>");
            html.Append("body{font-family:Arial, sans-serif; margin:30px; color:#000;}");
            html.Append(".header-officiel{text-align:center; font-weight:bold; margin-bottom:20px;}");
            html.Append("table{border-collapse:collapse; width:100%; margin-top:20px;}");
            html.Append("th,td{border:1px solid #333; padding:8px 12px; text-align:left;}");
            html.Append("th{background:#f0f2f5;}");
            html.Append("</style></head><body>");
            html.Append("<div class='header-officiel'>الجمهورية الجزائرية الديمقراطية الشعبية<br>FICHE D'ÉQUIPEMENT</div>");

            html.Append("<table>");
            foreach (DataGridViewColumn col in table_equipements.Columns)
            {
                if (col.Visible && !col.Name.StartsWith("col"))
                {
                    string val = row.Cells[col.Index].Value?.ToString() ?? "";
                    html.Append($"<tr><th>{WebUtility.HtmlEncode(col.HeaderText)}</th><td>{WebUtility.HtmlEncode(val)}</td></tr>");
                }
            }
            html.Append("</table></body></html>");

            string tempFile = Path.Combine(Path.GetTempPath(), $"fiche_equipement_{row.Cells["ID"].Value}.html");
            File.WriteAllText(tempFile, html.ToString());
            Process.Start(new ProcessStartInfo(tempFile) { UseShellExecute = true });
        }

        // =====================================================
        // SECTION 2 : FILTRAGE & RECHERCHE
        // =====================================================

        private void TextBoxfiltrage_TextChanged(object sender, EventArgs e) => AppliquerFiltre();
        private void listeDeFiltrage_SelectedIndexChanged(object sender, EventArgs e) => AppliquerFiltre();

        private void PeuplerListeFiltrage()
        {
            if (listeDeFIltrage.Items.Count > 0) return;

            listeDeFIltrage.DropDownStyle = ComboBoxStyle.DropDownList;
            foreach (var (affichage, _) in ColonnesFiltrablesEquipement)
                listeDeFIltrage.Items.Add(affichage);

            listeDeFIltrage.SelectedIndex = 0;
        }

        private void AppliquerFiltre()
        {
            if (table_equipements.DataSource is not DataTable dt) return;

            string recherche = TextBoxfiltrage.Text.Trim().Replace("'", "''");
            DataView vue = dt.DefaultView;

            if (string.IsNullOrEmpty(recherche))
            {
                vue.RowFilter = "";
                AfficherCompteur(dt.Rows.Count, dt.Rows.Count);
                return;
            }

            int index = listeDeFIltrage.SelectedIndex;
            string colonne = (index >= 0 && index < ColonnesFiltrablesEquipement.Length)
                ? ColonnesFiltrablesEquipement[index].Colonne : "";

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

            AfficherCompteur(vue.Count, dt.Rows.Count);
        }

        private void AfficherCompteur(int nbFiltre, int nbTotal)
        {
            if (lblCompteur == null) return;
            lblCompteur.Text = (nbFiltre == nbTotal)
                ? $"{nbTotal} équipement(s)"
                : $"{nbFiltre} affiché(s) sur {nbTotal}";
        }

        // =====================================================
        // SECTION 3 : IMPRESSION GLOBALE & NAVIGATION
        // =====================================================

        private void btnImprimer_Click(object sender, EventArgs e)
        {
            if (table_equipements.DataSource is not DataTable dt) return;

            int index = listeDeFIltrage.SelectedIndex;
            string colGroup = (index > 0 && index < ColonnesFiltrablesEquipement.Length)
                ? ColonnesFiltrablesEquipement[index].Colonne : "Catégorie";

            GenererRapportImprimable(dt, colGroup);
        }

        private void GenererRapportImprimable(DataTable dt, string colonneGroupement)
        {
            DataView vue = dt.DefaultView;
            var groupes = new SortedDictionary<string, List<DataRowView>>();

            foreach (DataRowView row in vue)
            {
                string cle = row[colonneGroupement]?.ToString();
                if (string.IsNullOrEmpty(cle)) cle = "(Non défini)";
                if (!groupes.ContainsKey(cle)) groupes[cle] = new List<DataRowView>();
                groupes[cle].Add(row);
            }

            if (groupes.Count == 0)
            {
                MessageBox.Show("Aucune donnée à imprimer.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var colsAffichees = new List<DataGridViewColumn>();
            foreach (DataGridViewColumn col in table_equipements.Columns)
            {
                if (col.Visible && !col.Name.StartsWith("col"))
                    colsAffichees.Add(col);
            }

            var html = new StringBuilder();
            html.Append("<html><head><meta charset='utf-8'><style>");
            html.Append("body{font-family:Arial, sans-serif; margin:25px; color:#000;}");
            html.Append(".header-officiel { font-family: Arial, serif; margin-bottom:25px; }");
            html.Append(".republique { font-size:16px; font-weight:bold; text-align:center; text-decoration:underline; margin-bottom:12px; }");
            html.Append(".ministere { font-size:13px; font-weight:bold; text-align:right; direction:rtl; line-height:1.6; }");
            html.Append(".divider { border-bottom:1.5px solid #000; margin:15px 0 20px 0; }");
            html.Append("h1{font-size:18px; text-align:center; color:#1a237e;}");
            html.Append("h2{font-size:13px; color:#1a237e; margin-top:20px;}");
            html.Append("table{border-collapse:collapse; width:100%; margin-bottom:15px;}");
            html.Append("th,td{border:1px solid #777; padding:5px 8px; font-size:11px; text-align:left;}");
            html.Append("th{background:#f0f2f5;}");
            html.Append("@media print{.no-print{display:none;}}");
            html.Append("</style></head><body>");

            html.Append("<div class='header-officiel'>");
            html.Append("  <div class='republique'>الجمهورية الجزائرية الديمقراطية الشعبية</div>");
            html.Append("  <div class='ministere'>");
            html.Append("    <div>وزارة الداخليـــــة و الجماعات المحلية .</div>");
            html.Append("    <div>ولايــــة غليزان </div>");
            html.Append("    <div>مديرية المواصلات السلكية و اللاسلكية الوطنية</div>");
            html.Append("    <div>مصلحة الادارة و الامداد / مكتب الوسائل العامة و المخزن.</div>");
            html.Append("  </div><div class='divider'></div></div>");

            html.Append($"<h1>Inventaire — groupé par {WebUtility.HtmlEncode(colonneGroupement)}</h1>");
            html.Append($"<div style='text-align:center;font-size:11px;'>Généré le {DateTime.Now:dd/MM/yyyy HH:mm} <span class='no-print'>— (Ctrl+P pour imprimer)</span></div>");

            foreach (var groupe in groupes)
            {
                html.Append($"<h2>{WebUtility.HtmlEncode(groupe.Key)} ({groupe.Value.Count} équipements)</h2><table><tr>");
                foreach (var col in colsAffichees)
                    html.Append($"<th>{WebUtility.HtmlEncode(col.HeaderText)}</th>");
                html.Append("</tr>");

                foreach (var row in groupe.Value)
                {
                    html.Append("<tr>");
                    foreach (var col in colsAffichees)
                    {
                        string field = string.IsNullOrEmpty(col.DataPropertyName) ? col.Name : col.DataPropertyName;
                        string val = dt.Columns.Contains(field) ? row[field]?.ToString() ?? "" : "";
                        html.Append($"<td>{WebUtility.HtmlEncode(val)}</td>");
                    }
                    html.Append("</tr>");
                }
                html.Append("</table>");
            }
            html.Append("</body></html>");

            string tempFile = Path.Combine(Path.GetTempPath(), $"rapport_{DateTime.Now:yyyyMMdd_HHmmss}.html");
            File.WriteAllText(tempFile, html.ToString());
            Process.Start(new ProcessStartInfo(tempFile) { UseShellExecute = true });
        }

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
        }

        private void btnToStockcontainer_Click(object sender, EventArgs e)
        {
            AfficherConteneur(stock_container);
            ChargerEquipements();
            if (stock_containers.SelectedTab == Modèlles)
            {
                ChargerFormulaireModeleDansTab();
            }
        }

        private void btnToRaportscontaine_Click(object sender, EventArgs e)
        {
            AfficherConteneur(repots_container);
        }
        private void btnChoisirColonnes_Click(object sender, EventArgs e)
        {
            var menu = new Guna.UI2.WinForms.Guna2ContextMenuStrip();
            foreach (DataGridViewColumn col in table_equipements.Columns)
            {
                var item = new ToolStripMenuItem(col.HeaderText) { Checked = col.Visible, CheckOnClick = true };
                item.Click += (s, args) => col.Visible = item.Checked;
                menu.Items.Add(item);
            }
            menu.Show(btnChoisirColonnes, new Point(0, btnChoisirColonnes.Height));
        }

        private void btnAjNouvEquip_Click(object sender, EventArgs e)
        {
            using (var frm = new FrmAjouterArticle(this))
            {
                if (frm.ShowDialog(this) == DialogResult.OK && frm.EquipementAjoute)
                    ChargerEquipements();
            }
        }

        private void table_equipements_Paint(object sender, PaintEventArgs e)
        {
            if (table_equipements.ColumnHeadersVisible && table_equipements.Columns.Count > 0)
            {
                int h = table_equipements.ColumnHeadersHeight;
                using (var pen = new Pen(Color.FromArgb(59, 130, 246), 2))
                    e.Graphics.DrawLine(pen, 0, h, table_equipements.Width, h);
            }
        }

        private void Form1_Load(object sender, EventArgs e) { }

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
                        ChargerEquipements();
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
                        DatabaseHelper.ExecuteNonQuery("DELETE FROM Ligne_mouvement WHERE mouvement_id = @id", new SqliteParameter("@id", mouvementId));
                        DatabaseHelper.ExecuteNonQuery("DELETE FROM Mouvement WHERE id = @id", new SqliteParameter("@id", mouvementId));
                        ChargerMouvements();
                        ChargerEquipements();
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

        private void ImprimerBonMouvement(DataGridViewRow row)
        {
            // 1. Récupération des données du DataGridView
            int mouvementId = Convert.ToInt32(row.Cells["ID"].Value);
            string refMouvement = row.Cells["Référence"].Value?.ToString() ?? row.Cells["N° Bon"].Value?.ToString() ?? "";
            string nomMouvement = row.Cells["Nom Mouvement"].Value?.ToString() ?? "وصل استلام";
            string nomEmploye = row.Cells["Employé"].Value?.ToString() ?? "";
            string deptEmploye = row.Cells["Département"].Value?.ToString() ?? "";
            string fonctionEmploye = row.Cells["Fonction"].Value?.ToString() ?? "";
            string fonctionEtDepartement = $"{fonctionEmploye} / {deptEmploye}";
            string dateMouvement = row.Cells["Date"].Value?.ToString() ?? "";
            string obsMouvement = row.Cells["Remarque"].Value?.ToString() ?? "";

            // Lecture du contenu texte (ou texte par défaut)
            string contenuMouvement = row.Cells["Contenu"].Value?.ToString();
            if (string.IsNullOrWhiteSpace(contenuMouvement))
            {
                contenuMouvement = "أصرح بأني استلمت من السيد(ة) المكلف(ة) بتسيير مكتب الوسائل العامة والمخزن بمديرية المواصلات السلكية واللاسلكية، العتاد المبين في الجدول أدناه:";
            }

            // 2. Récupération des lignes de mouvement (équipements)
            var lignesSortie = new List<Dictionary<string, string>>();
            var lignesEntree = new List<Dictionary<string, string>>();

            string sqlLignes = @"
        SELECT 
            lm.est_sortie, lm.etat_a_la_mouvement, lm.observation AS obs_ligne,
            eq.numero_serie, eq.code_barre,
            mod.designation AS designation_modele, mod.reference AS reference_modele,
            mrq.designation AS marque_nom, cat.designation AS famille_nom
        FROM Ligne_mouvement lm
        JOIN Equipement eq ON lm.equipement_id = eq.id
        JOIN Modele mod ON eq.modele_id = mod.id
        LEFT JOIN Marque mrq ON mod.marque_id = mrq.id
        LEFT JOIN Categorie cat ON mod.categorie_id = cat.id
        WHERE lm.mouvement_id = @id";

            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                using (var cmd = new Microsoft.Data.Sqlite.SqliteCommand(sqlLignes, conn))
                {
                    cmd.Parameters.AddWithValue("@id", mouvementId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var item = new Dictionary<string, string>
                            {
                                ["famille_nom"] = reader["famille_nom"]?.ToString() ?? "",
                                ["marque_nom"] = reader["marque_nom"]?.ToString() ?? "",
                                ["designation_modele"] = reader["designation_modele"]?.ToString() ?? "",
                                ["reference_modele"] = reader["reference_modele"]?.ToString() ?? "",
                                ["numero_serie"] = reader["numero_serie"]?.ToString() ?? "",
                                ["code_barre"] = reader["code_barre"]?.ToString() ?? "",
                                ["etat_a_la_mouvement"] = reader["etat_a_la_mouvement"]?.ToString() ?? "",
                                ["obs_ligne"] = reader["obs_ligne"]?.ToString() ?? ""
                            };

                            if (Convert.ToInt32(reader["est_sortie"]) == 1)
                                lignesSortie.Add(item);
                            else
                                lignesEntree.Add(item);
                        }
                    }
                }
            }

            // 3. Construction du document HTML
            var html = new StringBuilder();
            html.Append("<!DOCTYPE html><html dir='rtl' lang='ar'><head><meta charset='utf-8'><style>");

            // Styles CSS basés sur votre structure
            html.Append("body { font-family:  Arial, sans-serif; margin: 25px; color: #000; direction: rtl; text-align: right; }");
            html.Append(".header-officiel { font-family: Arial, serif; margin-bottom: 20px; }");
            html.Append(".republique {text-align:center; font-size:24px; font-weight:bold; text-decoration:underline; margin-bottom:15px; direction:rtl;}");

            html.Append(".top-container { display: flex; justify-content: space-between; align-items: flex-start; }");
            html.Append(".ref-box { font-size: 13px; font-weight: bold; text-align: left; direction: ltr; padding-top: 10px; }");
            html.Append(".ministere{text-align:right; font-size:16px; font-weight:bold;margin-bottom:15px; direction:rtl;}");

            html.Append(".title-container { text-align: center; margin: 25px 0 20px 0; }");
            html.Append(".title-box { display: inline-block; border: 1.5px solid #000; padding: 5px 35px; font-size: 22px; font-weight: bold; }");

            html.Append(".info-section { font-size: 14px; line-height: 1.8; margin-bottom: 15px; font-weight: bold; }");
            html.Append(".info-row { margin-bottom: 4px; }");
            html.Append(".contenu-text { font-size: 14px; font-weight: normal; margin: 15px 0 20px 0; text-align: justify; line-height: 1.6; }");

            html.Append(".table-title { font-size: 14px; font-weight: bold; margin-top: 18px; margin-bottom: 6px; }");
            html.Append("table { border-collapse: collapse; width: 100%; margin-bottom: 15px; direction: rtl; }");
            html.Append("th, td { border: 1px solid #000; padding: 6px 8px; font-size: 11px; text-align: center; color: #000; }");
            html.Append("th { background: #f0f2f5; font-weight: bold; }");

            html.Append(".obs-section { font-size: 14px; margin-top: 15px; font-weight: bold; }");
            html.Append(".signatures-table { width: 100%; border: none; margin-top: 40px; }");
            html.Append(".signatures-table td { border: none; font-size: 14px; font-weight: bold; text-align: center; width: 50%; vertical-align: top; height: 100px; }");

            html.Append("@media print { .no-print { display: none; } }");
            html.Append("</style></head><body>");

            // En-tête administratif
            html.Append("<div class='header-officiel'>");
            html.Append("  <div class='republique'>الجمهورية الجزائرية الديمقراطية الشعبية</div>");
            html.Append("  <div class='top-container'>");
            html.Append("    <div class='ministere'>");
            html.Append("      <div>وزارة الداخليـــــة و الجماعات المحلية .</div>");
            html.Append("      <div>ولايــــة غليزان </div>");
            html.Append("      <div>مديرية المواصلات السلكية و اللاسلكية الوطنية</div>");
            html.Append("      <div>مصلحة الادارة و الامداد / مكتب الوسائل العامة و المخزن.</div>");
            html.Append("    </div>");
            html.Append("  </div>");
            html.Append($"   <div class='ref-box'>Réf : {WebUtility.HtmlEncode(refMouvement)}</div>");
            html.Append("</div>");

            // Titre encadré
            html.Append("<div class='title-container'>");
            html.Append($"  <span class='title-box'>- {WebUtility.HtmlEncode(nomMouvement)} -</span>");
            html.Append("</div>");

            // Informations de l'employé
            html.Append("<div class='info-section'>");
            html.Append($"  <div class='info-row'>انا الممضي اسفله : {WebUtility.HtmlEncode(nomEmploye)}</div>");
            html.Append($"  <div class='info-row'>الوظيفة : {WebUtility.HtmlEncode(fonctionEtDepartement)}</div>");
            html.Append($"  <div class='info-row'>بتاريخ : {WebUtility.HtmlEncode(dateMouvement)}</div>");
            html.Append("</div>");

            // Contenu explicatif
            if (!string.IsNullOrWhiteSpace(contenuMouvement))
            {
                html.Append($"<div class='contenu-text'>{WebUtility.HtmlEncode(contenuMouvement)}</div>");
            }

            // Fonction d'impression des tableaux
            void GenererTableauMatériel(List<Dictionary<string, string>> items, string titreSection)
            {
                if (items.Count == 0) return;

                html.Append($"<div class='table-title'>{titreSection}</div>");
                html.Append("<table><tr>");
                // Colonnes orientées de droite à gauche (RTL)
                html.Append("<th style='width:5%;'>QTE</th>");
                html.Append("<th>Famille</th>");
                html.Append("<th>Marque</th>");
                html.Append("<th>Designniation de modèlle</th>");
                html.Append("<th>Reference modèle</th>");
                html.Append("<th>Numero serie</th>");
                html.Append("<th>Code barre</th>");
                html.Append("<th>Etat</th>");
                html.Append("<th>observation</th>");
                html.Append("</tr>");

                foreach (var r in items)
                {
                    html.Append("<tr>");
                    html.Append("<td>01</td>");
                    html.Append($"<td>{WebUtility.HtmlEncode(r["famille_nom"])}</td>");
                    html.Append($"<td>{WebUtility.HtmlEncode(r["marque_nom"])}</td>");
                    html.Append($"<td>{WebUtility.HtmlEncode(r["designation_modele"])}</td>");
                    html.Append($"<td>{WebUtility.HtmlEncode(r["reference_modele"])}</td>");
                    html.Append($"<td>{WebUtility.HtmlEncode(r["numero_serie"])}</td>");
                    html.Append($"<td>{WebUtility.HtmlEncode(r["code_barre"])}</td>");
                    html.Append($"<td>{WebUtility.HtmlEncode(r["etat_a_la_mouvement"])}</td>");
                    html.Append($"<td>{WebUtility.HtmlEncode(r["obs_ligne"])}</td>");
                    html.Append("</tr>");
                }
                html.Append("</table>");
            }

            // Tableaux de matériel (Mأخوذ et المرجع)
            GenererTableauMatériel(lignesSortie, "العتاد المأخوذ :");
            GenererTableauMatériel(lignesEntree, "العتاد المرجع :");

            // Remarque / Observation globale
            if (!string.IsNullOrWhiteSpace(obsMouvement))
            {
                html.Append($"<div class='obs-section'>ملاحظة : {WebUtility.HtmlEncode(obsMouvement)}</div>");
            }

            // Zone des Signatures
            html.Append("<table class='signatures-table'><tr>");
            html.Append("  <td>إمضاء المكلف(ة) بمكتب الوسائل العامة و المخزن :</td>");
            html.Append("  <td>إمضاء المستلم(ة):</td>");
            html.Append("</tr></table>");

            html.Append("</body></html>");

            // Sauvegarde du fichier temporaire et ouverture
            string tempFile = Path.Combine(Path.GetTempPath(), $"bon_mouvement_{mouvementId}_{DateTime.Now:yyyyMMdd_HHmmss}.html");
            File.WriteAllText(tempFile, html.ToString(), Encoding.UTF8);
            Process.Start(new ProcessStartInfo(tempFile) { UseShellExecute = true });
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
                    ChargerEquipements();
                }
            }
        }





        // =====================================================
        // SECTION 5 : INVENTAIRES
        // =====================================================

        public void ChargerInventaires()
        {
            string sql = @"
                SELECT 
                    i.id AS 'ID',
                    i.structure AS 'Structure',
                    i.bureau AS 'Bureau',
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
                using (var frm = new FrmAjouterInventaire(this, inventaireId))
                {
                    if (frm.ShowDialog(this) == DialogResult.OK)
                        ChargerInventaires();
                }
            }
            else if (colName == "colSupprimerINV")
            {
                string bureau = grid.Rows[e.RowIndex].Cells["Bureau"].Value?.ToString() ?? "";
                var confirm = MessageBox.Show(
                    $"Voulez-vous vraiment supprimer la fiche d'inventaire du bureau '{bureau}' ?\nCette action supprimera également toutes ses lignes.",
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

            string sqlLignes = @"
                SELECT li.quantite, li.observation, m.designation AS modele
                FROM Ligne_inventaire li
                JOIN Equipement e ON li.equipement_id = e.id
                JOIN Modele m ON e.modele_id = m.id
                WHERE li.inventaire_id = @id
                ORDER BY li.id";

            DataTable lignes = DatabaseHelper.ExecuteQuery(sqlLignes, new SqliteParameter("@id", inventaireId));

            var html = new StringBuilder();
            html.Append("<html><head><meta charset='utf-8'><style>");
            html.Append("body{font-family:Arial, sans-serif; margin:35px; color:#000;}");
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
            html.Append("<div class='infos'>STRUCTURE : " + WebUtility.HtmlEncode(structure) + "</div>");
            html.Append("<div class='infos'>BUREAU : " + WebUtility.HtmlEncode(bureau) + "</div>");
            html.Append("<div class='date'>" + WebUtility.HtmlEncode(date) + "</div>");

            html.Append("<div class='titre-box'><span>FICHE D'INVENTAIRE</span></div>");

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

        private void btnNouveauInventaire_Click(object sender, EventArgs e)
        {
            using (var frm = new FrmAjouterInventaire(this))
            {
                if (frm.ShowDialog(this) == DialogResult.OK && frm.InventaireEnregistre)
                {
                    ChargerInventaires();
                }
            }
        }

        private void stockHeaderPanel_Paint(object sender, PaintEventArgs e)
        {

        }

        private void tabPage1_Click(object sender, EventArgs e)
        {

        }
        //******************************************************************
        #region Authentification & Gestion utilisateurs
        //******************************************************************

        // Affiche le nom de l'utilisateur connecté dans le Label
        // "lblUtilisateurConnecte" (à créer dans le Designer si absent).
        private void AfficherInfoUtilisateur()
        {
            if (lblUtilisateurConnecte != null)
                lblUtilisateurConnecte.Text =SessionUtilisateur.NomAffichage;
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
    }
}