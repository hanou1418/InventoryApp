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

namespace InventoryApp
{
    public partial class Form1 : Form
    {
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

        public Form1()
        {
            InitializeComponent();

            // Configuration DataGridView Equipements
            table_equipements.RowTemplate.Height = 38;
            table_equipements.CellPainting += Table_equipements_CellPainting;
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

            AfficherConteneur(home_container);
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
                    e.date_acquisition AS 'Date Acquisition'
                FROM Equipement e
                JOIN Modele m ON e.modele_id = m.id
                LEFT JOIN Marque mq ON m.marque_id = mq.id
                LEFT JOIN Categorie c ON m.categorie_id = c.id
                ORDER BY e.id DESC";

            table_equipements.AutoGenerateColumns = true;
            table_equipements.DataSource = DatabaseHelper.ExecuteQuery(sql);

            if (table_equipements.Columns.Contains("Utilisé par"))
                table_equipements.Columns["Utilisé par"].ValueType = typeof(string);

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
                    Color.FromArgb(239, 246, 255), Color.FromArgb(219, 234, 254), Color.FromArgb(191, 219, 254),
                    Color.FromArgb(147, 197, 253), "print_icon.png", iconSize);
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
            html.Append("    <div>مصلحة الصيانة / مكتب الوسائل العامة و المخزن.</div>");
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
            home_container.Visible = ReferenceEquals(conteneurActif, home_container);
            stock_container.Visible = ReferenceEquals(conteneurActif, stock_container);
            repots_container.Visible = ReferenceEquals(conteneurActif, repots_container);
        }

        private void btnToAccueilcontainer_Click(object sender, EventArgs e) => AfficherConteneur(home_container);
        private void btnToStockcontainer_Click(object sender, EventArgs e) { AfficherConteneur(stock_container); ChargerEquipements(); }
        private void btnToRaportscontaine_Click(object sender, EventArgs e) => AfficherConteneur(repots_container);

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
                    m.type_mouvement AS 'Type',
                    m.date_mouvement AS 'Date',
                    (emp.nom || ' ' || emp.prenom) AS 'Employé',
                    COALESCE(emp.departement, 'Sans Service') AS 'Département',
                    m.observation AS 'Remarque'
                FROM Mouvement m
                LEFT JOIN Employe emp ON m.employe_id = emp.id
                ORDER BY m.id DESC";

            DataTable dt = DatabaseHelper.ExecuteQuery(sql);
            tableMVMDataGridView.AutoGenerateColumns = true;
            tableMVMDataGridView.DataSource = dt;

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
                    Color.FromArgb(239, 246, 255), Color.FromArgb(219, 234, 254), Color.FromArgb(191, 219, 254),
                    Color.FromArgb(147, 197, 253), "print_icon.png", iconSize);
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
            var html = new StringBuilder();
            html.Append("<html><head><meta charset='utf-8'><style>");
            html.Append("body{font-family:Arial, sans-serif; margin:30px; color:#000;}");
            html.Append(".header-officiel{text-align:center; font-weight:bold; margin-bottom:20px;}");
            html.Append("table{border-collapse:collapse; width:100%; margin-top:20px;}");
            html.Append("th,td{border:1px solid #333; padding:8px 12px; text-align:left;}");
            html.Append("th{background:#f0f2f5;}");
            html.Append("</style></head><body>");
            html.Append("<div class='header-officiel'>الجمهورية الجزائرية الديمقراطية الشعبية<br>BON DE MOUVEMENT</div>");

            html.Append("<table>");
            foreach (DataGridViewColumn col in tableMVMDataGridView.Columns)
            {
                if (col.Visible && !col.Name.StartsWith("col"))
                {
                    string val = row.Cells[col.Index].Value?.ToString() ?? "";
                    html.Append($"<tr><th>{WebUtility.HtmlEncode(col.HeaderText)}</th><td>{WebUtility.HtmlEncode(val)}</td></tr>");
                }
            }
            html.Append("</table></body></html>");

            string tempFile = Path.Combine(Path.GetTempPath(), $"bon_mouvement_{row.Cells["ID"].Value}.html");
            File.WriteAllText(tempFile, html.ToString());
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
    }
}