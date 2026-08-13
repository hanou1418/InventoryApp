using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using InventoryApp.Data;
using Microsoft.Data.Sqlite;

namespace InventoryApp
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

            table_equipements.RowTemplate.Height = 38;
            table_equipements.CellPainting += Table_equipements_CellPainting;
            table_equipements.CellMouseMove += (s, e) => table_equipements.InvalidateCell(e.ColumnIndex, e.RowIndex);
            table_equipements.CellMouseLeave += (s, e) => table_equipements.InvalidateCell(e.ColumnIndex, e.RowIndex);

            ChargerEquipements();

            AfficherConteneur(home_container);
        }

        private void guna2Button1_Click(object sender, EventArgs e)
        {

        }

        private void guna2Button2_Click(object sender, EventArgs e)
        {

        }

        private void flowLayoutPanel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void guna2Panel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void guna2Button1_Click_1(object sender, EventArgs e)
        {

        }

        private void guna2Panel3_Paint(object sender, PaintEventArgs e)
        {

        }

        private void guna2HtmlLabel1_Click(object sender, EventArgs e)
        {

        }

        private void guna2Button2_Click_1(object sender, EventArgs e)
        {

        }

        // =====================================================
        // FILTRAGE : recherche libre ou par attribut précis
        // Controles reels : TextBoxfiltrage (Guna2TextBox) et
        // listeDeFiltrage (Guna2ComboBox) -- voir stockHeaderPanel
        // =====================================================
        private void TextBoxfiltrage_TextChanged(object sender, EventArgs e)
        {
            AppliquerFiltre();
        }

        private void listeDeFiltrage_SelectedIndexChanged(object sender, EventArgs e)
        {
            AppliquerFiltre();
        }

        // Liste des colonnes filtrables : (texte affiché, nom exact de la colonne)
        private static readonly (string Affichage, string Colonne)[] ColonnesFiltrables = new[]
        {
            ("Tous les champs", ""),
            ("Modèle",           "Modèle"),
            ("N° Série",         "N° Série"),
            ("Référence Modèle", "Référence Modèle"),
            ("Marque",           "Marque"),
            ("Catégorie",        "Catégorie"),
            ("Statut",           "Statut"),
            ("Code-Barre",       "Code-Barre"),
        };

        private void PeuplerListeFiltrage()
        {
            // On ne repeuple que si la liste est vide, pour ne pas perdre
            // la sélection de l'utilisateur à chaque rechargement du tableau.
            if (listeDeFIltrage.Items.Count > 0) return;

            listeDeFIltrage.DropDownStyle = ComboBoxStyle.DropDownList;
            foreach (var (affichage, _) in ColonnesFiltrables)
                listeDeFIltrage.Items.Add(affichage);

            listeDeFIltrage.SelectedIndex = 0; // "Tous les champs" par défaut
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
            string colonneChoisie = (index >= 0 && index < ColonnesFiltrables.Length)
                ? ColonnesFiltrables[index].Colonne
                : "";

            if (string.IsNullOrEmpty(colonneChoisie))
            {
                // "Tous les champs" : recherche dans toutes les colonnes, reliees par OR.
                var conditions = new System.Collections.Generic.List<string>();
                foreach (DataColumn col in dt.Columns)
                    conditions.Add($"CONVERT([{col.ColumnName}], 'System.String') LIKE '%{recherche}%'");

                vue.RowFilter = string.Join(" OR ", conditions);
            }
            else
            {
                vue.RowFilter = $"CONVERT([{colonneChoisie}], 'System.String') LIKE '%{recherche}%'";
            }

            AfficherCompteur(vue.Count, dt.Rows.Count);
        }

        // Affiche "X équipement(s) affiché(s) sur Y". Si "lblCompteur" n'existe
        // pas encore dans le Designer, cette methode ne fait simplement rien
        // (aucun crash) -- ajoute le Label plus tard si tu veux ce compteur visible.
        private void AfficherCompteur(int nbFiltre, int nbTotal)
        {
            if (lblCompteur == null) return;
            lblCompteur.Text = (nbFiltre == nbTotal)
                ? $"{nbTotal} équipement(s)"
                : $"{nbFiltre} équipement(s) affiché(s) sur {nbTotal}";
        }

        // =====================================================
        // Chargement du tableau : exactement les colonnes demandees
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
                    e.statut AS 'Statut',
                    e.etat AS 'État',
                    e.code_barre AS 'Code-Barre',
                    e.date_acquisition AS 'Date Acquisition',
                    e.date_modification AS 'Date Modification'
                FROM Equipement e
                JOIN Modele m ON e.modele_id = m.id
                LEFT JOIN Marque mq ON m.marque_id = mq.id
                LEFT JOIN Categorie c ON m.categorie_id = c.id
                ORDER BY e.id DESC";

            table_equipements.AutoGenerateColumns = true;
            table_equipements.DataSource = DatabaseHelper.ExecuteQuery(sql);

            AjouterColonnesActions();
            PeuplerListeFiltrage();
            AppliquerFiltre(); // réapplique le filtre en cours, s'il y en a un
        }

        private void AjouterColonnesActions()
        {
            if (table_equipements.Columns.Contains("colModifier"))
                table_equipements.Columns.Remove("colModifier");
            if (table_equipements.Columns.Contains("colSupprimer"))
                table_equipements.Columns.Remove("colSupprimer");

            var colModifier = new DataGridViewButtonColumn
            {
                Name = "colModifier",
                HeaderText = "Modifier",
                Width = 70,
                FlatStyle = FlatStyle.Flat
            };
            table_equipements.Columns.Add(colModifier);

            var colSupprimer = new DataGridViewButtonColumn
            {
                Name = "colSupprimer",
                HeaderText = "Supprimer",
                Width = 70,
                FlatStyle = FlatStyle.Flat
            };
            table_equipements.Columns.Add(colSupprimer);
        }

        private void Table_equipements_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0) return;

            Point mousePos = table_equipements.PointToClient(Cursor.Position);
            bool isHovered = e.CellBounds.Contains(mousePos);
            bool isClicked = isHovered && (Control.MouseButtons == MouseButtons.Left);

            int iconSize = 18;

            if (e.ColumnIndex == table_equipements.Columns["colModifier"].Index)
            {
                e.PaintBackground(e.CellBounds, true);

                Color bgColor = Color.FromArgb(240, 253, 244);
                if (isClicked) bgColor = Color.FromArgb(187, 247, 208);
                else if (isHovered) bgColor = Color.FromArgb(220, 252, 231);

                Rectangle btnRect = new Rectangle(e.CellBounds.Left + 4, e.CellBounds.Top + 4, e.CellBounds.Width - 8, e.CellBounds.Height - 8);
                using (SolidBrush brush = new SolidBrush(bgColor))
                {
                    e.Graphics.FillRectangle(brush, btnRect);
                }

                using (Pen pen = new Pen(Color.FromArgb(134, 239, 172)))
                {
                    e.Graphics.DrawRectangle(pen, btnRect);
                }

                string pencilPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "image", "pencil_icon.png");
                if (!System.IO.File.Exists(pencilPath)) pencilPath = "image/pencil_icon.png";

                if (System.IO.File.Exists(pencilPath))
                {
                    using (Image imgPencil = Image.FromFile(pencilPath))
                    {
                        int x = btnRect.Left + (btnRect.Width - iconSize) / 2;
                        int y = btnRect.Top + (btnRect.Height - iconSize) / 2;
                        e.Graphics.DrawImage(imgPencil, new Rectangle(x, y, iconSize, iconSize));
                    }
                }

                e.Handled = true;
            }

            if (e.ColumnIndex == table_equipements.Columns["colSupprimer"].Index)
            {
                e.PaintBackground(e.CellBounds, true);

                Color bgColor = Color.FromArgb(254, 242, 242);
                if (isClicked) bgColor = Color.FromArgb(254, 202, 202);
                else if (isHovered) bgColor = Color.FromArgb(254, 226, 226);

                Rectangle btnRect = new Rectangle(e.CellBounds.Left + 4, e.CellBounds.Top + 4, e.CellBounds.Width - 8, e.CellBounds.Height - 8);
                using (SolidBrush brush = new SolidBrush(bgColor))
                {
                    e.Graphics.FillRectangle(brush, btnRect);
                }

                using (Pen pen = new Pen(Color.FromArgb(252, 165, 165)))
                {
                    e.Graphics.DrawRectangle(pen, btnRect);
                }

                string deletePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "image", "delet_icon.png");
                if (!System.IO.File.Exists(deletePath)) deletePath = "image/delet_icon.png";

                if (System.IO.File.Exists(deletePath))
                {
                    using (Image imgDelete = Image.FromFile(deletePath))
                    {
                        int x = btnRect.Left + (btnRect.Width - iconSize) / 2;
                        int y = btnRect.Top + (btnRect.Height - iconSize) / 2;
                        e.Graphics.DrawImage(imgDelete, new Rectangle(x, y, iconSize, iconSize));
                    }
                }

                e.Handled = true;
            }
        }

        private void btnAjNouvEquip_Click(object sender, EventArgs e)
        {
            using (var frm = new FrmAjouterArticle(this))
            {
                if (frm.ShowDialog(this) == DialogResult.OK && frm.EquipementAjoute)
                {
                    ChargerEquipements();
                }
            }
        }

        private void table_equipements_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            var grid = (DataGridView)sender;
            string colName = grid.Columns[e.ColumnIndex].Name;

            if (colName == "colModifier")
            {
                int equipementId = Convert.ToInt32(grid.Rows[e.RowIndex].Cells["ID"].Value);
                using (var frm = new FrmAjouterArticle(this, equipementId))
                {
                    if (frm.ShowDialog(this) == DialogResult.OK)
                    {
                        ChargerEquipements();
                    }
                }
            }
            else if (colName == "colSupprimer")
            {
                int equipementId = Convert.ToInt32(grid.Rows[e.RowIndex].Cells["ID"].Value);
                string modele = grid.Rows[e.RowIndex].Cells["Modèle"].Value?.ToString() ?? "";

                var confirm = MessageBox.Show(
                    $"Voulez-vous vraiment supprimer l'équipement #{equipementId} ({modele}) ?\nCette action est irréversible.",
                    "Confirmer la suppression",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (confirm == DialogResult.Yes)
                {
                    try
                    {
                        DatabaseHelper.ExecuteNonQuery(
                            "DELETE FROM Equipement WHERE id = @id",
                            new SqliteParameter("@id", equipementId));

                        ChargerEquipements();
                    }
                    catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
                    {
                        MessageBox.Show(
                            "Impossible de supprimer : cet équipement est référencé dans un historique de mouvement.\n" +
                            "Envisagez de le marquer comme 'Réformé' plutôt que de le supprimer.",
                            "Suppression refusée", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void guna2Panel3_Paint_1(object sender, PaintEventArgs e)
        {

        }

        private void table_equipements_CausesValidationChanged(object sender, EventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void table_equipements_Paint(object sender, PaintEventArgs e)
        {
            if (table_equipements.ColumnHeadersVisible && table_equipements.Columns.Count > 0)
            {
                int headerHeight = table_equipements.ColumnHeadersHeight;
                using (Pen pen = new Pen(Color.FromArgb(59, 130, 246), 2))
                {
                    e.Graphics.DrawLine(pen, 0, headerHeight, table_equipements.Width, headerHeight);
                }
            }
        }

        // =====================================================
        // Rapport imprimable : groupe les lignes ACTUELLEMENT FILTREES
        // par attribut choisi, une table par valeur, titrée par cette
        // valeur (ex: "Imprimante :"). Ouvre le résultat dans le
        // navigateur par défaut pour impression (Ctrl+P) ou export PDF.
        // =====================================================
        private void btnImprimer_Click(object sender, EventArgs e)
        {
            if (table_equipements.DataSource is not DataTable dt) return;

            int index = listeDeFIltrage.SelectedIndex;
            string colonneGroupement = (index > 0 && index < ColonnesFiltrables.Length)
                ? ColonnesFiltrables[index].Colonne
                : "Catégorie";

            GenererRapportImprimable(dt, colonneGroupement);
        }

        private void GenererRapportImprimable(DataTable dt, string colonneGroupement)
        {
            DataView vue = dt.DefaultView;

            var groupes = new System.Collections.Generic.SortedDictionary<string, System.Collections.Generic.List<DataRowView>>();
            foreach (DataRowView row in vue)
            {
                string cle = row[colonneGroupement]?.ToString();
                if (string.IsNullOrEmpty(cle)) cle = "(Non défini)";
                if (!groupes.ContainsKey(cle))
                    groupes[cle] = new System.Collections.Generic.List<DataRowView>();
                groupes[cle].Add(row);
            }

            if (groupes.Count == 0)
            {
                MessageBox.Show("Aucune donnée à imprimer avec le filtre actuel.", "Rapport vide",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var colonnesAffichees = new[]
            {
                "ID", "Catégorie","Marque","Modèle", "Référence Modèle","N° Série",
                  "Statut","État", "Code-Barre", "Date Acquisition"
            };

            var html = new System.Text.StringBuilder();
            html.Append("<html><head><meta charset='utf-8'><style>");
            html.Append("body{font-family:Arial, sans-serif; margin:25px; color:#000;}");

            // --- Police Arabe Traditionnelle Officielle (Style exact de la photo) ---
            html.Append(".header-officiel { font-family: Arial, 'Times New Roman', serif; margin-bottom: 25px; }");
            html.Append(".republique { font-size: 16px; font-weight: bold; text-align: center; text-decoration: underline; text-underline-offset: 4px; margin-bottom: 12px; letter-spacing: 0.5px; }");
            html.Append(".ministere { font-size: 13px; font-weight: bold; text-align: right; direction: rtl; line-height: 1.6; }");
            html.Append(".divider { border-bottom: 1.5px solid #000; margin-top: 15px; margin-bottom: 20px; }");

            // --- Contenu & Groupes (SANS RECTANGLES PLEINS) ---
            html.Append("h1{font-size:18px; text-align:center; color:#1a237e; margin:15px 0 5px 0;}");
            html.Append(".info-date{font-size:11px; color:#333; text-align:center; margin-bottom:25px;}");

            // Style de titre de groupe : Simple et professionnel (pas de fond bleu opaque)
            html.Append("h2{font-size:14px; color:#1a237e; border-bottom:2px solid #1a237e; padding-bottom:3px; margin-top:25px; margin-bottom:10px;}");

            html.Append("table{border-collapse:collapse; width:100%; margin-bottom:15px;}");
            html.Append("th,td{border:1px solid #777; padding:5px 8px; font-size:11px; text-align:left;}");
            html.Append("th{background:#f0f2f5; font-weight:bold;}");

            // Gestion spécifique de l'impression
            html.Append("@media print{");
            html.Append("  body{margin:10mm;}");
            html.Append("  .no-print{display:none;}"); // Cache uniquement les instructions de touches
            html.Append("}");

            html.Append("</style></head><body>");

            // --- En-tête administratif officiel ---
            html.Append("<div class='header-officiel'>");
            html.Append("  <div class='republique'>الجمهورية الجزائرية الديمقراطية الشعبية</div>");
            html.Append("  <div class='ministere'>");
            html.Append("    <div>وزارة الداخليـــــة و الجماعات المحلية .</div>");
            html.Append("    <div>ولايــــة غليزان </div>");
            html.Append("    <div>مديرية المواصلات السلكية و اللاسلكية الوطنية</div>");
            html.Append("    <div>مصلحة الصيانة / مكتب الوسائل العامة و المخزن.</div>");
            html.Append("  </div>");
            html.Append("  <div class='divider'></div>");
            html.Append("</div>");

            // --- Titre du document ---
            html.Append($"<h1>Inventaire — groupé par {System.Net.WebUtility.HtmlEncode(colonneGroupement)}</h1>");

            // La date reste visible à l'impression, seul le texte "appuyez sur Ctrl+P..." disparaît
            html.Append($"<div class='info-date'>Généré le {DateTime.Now:dd/MM/yyyy HH:mm} <span class='no-print'>— (Appuyez sur Ctrl+P pour imprimer)</span></div>");

            // --- Listes des équipements ---
            foreach (var groupe in groupes)
            {
                html.Append($" {System.Net.WebUtility.HtmlEncode(groupe.Key)} : ({groupe.Value.Count} équipement(s))");
                html.Append("<table><tr>");
                foreach (var col in colonnesAffichees) html.Append($"<th>{col}</th>");
                html.Append("</tr>");

                foreach (var row in groupe.Value)
                {
                    html.Append("<tr>");
                    foreach (var col in colonnesAffichees)
                        html.Append($"<td>{System.Net.WebUtility.HtmlEncode(row[col]?.ToString() ?? "")}</td>");
                    html.Append("</tr>");
                }
                html.Append("</table>");
            }

            html.Append("</body></html>");

            string tempFile = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                $"rapport_inventaire_{DateTime.Now:yyyyMMdd_HHmmss}.html");
            System.IO.File.WriteAllText(tempFile, html.ToString());

            var psi = new System.Diagnostics.ProcessStartInfo(tempFile) { UseShellExecute = true };
            System.Diagnostics.Process.Start(psi);
        }



        // =====================================================
        // Navigation : chaque bouton du menu affiche son conteneur
        // et cache les deux autres.
        // =====================================================
        private void AfficherConteneur(Control conteneurActif)
        {
            home_container.Visible = ReferenceEquals(conteneurActif, home_container);
            stock_container.Visible = ReferenceEquals(conteneurActif, stock_container);
            repots_container.Visible = ReferenceEquals(conteneurActif, repots_container);
        }

        private void btnToAccueilcontainer_Click(object sender, EventArgs e)
        {
            AfficherConteneur(home_container);
        }

        private void btnToStockcontainer_Click(object sender, EventArgs e)
        {
            AfficherConteneur(stock_container);
            ChargerEquipements();
        }

        private void btnToRaportscontaine_Click(object sender, EventArgs e)
        {
            AfficherConteneur(repots_container);
        }

    }
}