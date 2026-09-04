#nullable enable
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Text;
using System.Windows.Forms;
using Guna.UI2.WinForms;
using InventoryApp.Data;
using Microsoft.Data.Sqlite;

namespace InventoryApp
{
    public class FrmGererMarques : Form
    {
        private readonly Form1? _mainForm;

        public int? DerniereMarqueModifieeId { get; private set; } = null;

        // Bandeau d'actions principal
        private Guna2Panel headerPanel = null!;
        private Guna2TextBox filtreTextBox = null!;
        private Guna2ComboBox listeFiltrageComboBox = null!;
        private Guna2HtmlLabel lblCompteur = null!;
        private FlowLayoutPanel rightActionsPanel = null!;
        private Guna2Button btnChoisirColonnes = null!;
        private Guna2Button btnImprimer = null!;
        private Guna2Button btnNouvelleMarque = null!;
        private Guna2DataGridView tableMarquesDataGridView = null!;

        private static readonly (string Affichage, string Colonne)[] ColonnesFiltrablesMarque = new[]
        {
            ("Tous les champs", ""),
            ("Code",            "Code"),
            ("Désignation",     "Désignation")
        };

        public FrmGererMarques(Form1? mainForm = null)
        {
            _mainForm = mainForm;

            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.White;
            Dock = DockStyle.Fill;

            ConstruireControles();
            Load += (s, e) => ChargerListe();
        }

        private void ConstruireControles()
        {
            // 1. En-tête bleu sombre
            headerPanel = new Guna2Panel
            {
                Dock = DockStyle.Top,
                Height = 46,
                FillColor = Color.FromArgb(24, 30, 54)
            };

            // Zone de recherche
            filtreTextBox = new Guna2TextBox
            {
                Location = new Point(3, 5),
                Size = new Size(346, 36),
                BorderColor = Color.FromArgb(37, 99, 235),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(24, 30, 54),
                PlaceholderText = "Rechercher une marque . . . . .",
                IconRight = ChargerImageLocale("search.png"),
                IconRightSize = new Size(30, 30),
                IconRightOffset = new Point(0, 0)
            };
            filtreTextBox.TextChanged += (s, e) => AppliquerFiltre();

            // ComboBox de filtre
            listeFiltrageComboBox = new Guna2ComboBox
            {
                Location = new Point(352, 5),
                Size = new Size(151, 36),
                BackColor = Color.Transparent,
                BorderColor = Color.FromArgb(37, 99, 235),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(24, 30, 54)
            };
            foreach (var (affichage, _) in ColonnesFiltrablesMarque)
                listeFiltrageComboBox.Items.Add(affichage);
            listeFiltrageComboBox.SelectedIndex = 0;
            listeFiltrageComboBox.SelectedIndexChanged += (s, e) => AppliquerFiltre();

            // Compteur
            lblCompteur = new Guna2HtmlLabel
            {
                Location = new Point(509, 12),
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.White,
                Text = "0 marque(s)"
            };

            // Panneau conteneur aligné à droite pour les boutons d'actions
            rightActionsPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 5, 3, 0),
                WrapContents = false
            };

            // Bouton Sélection de colonnes
            btnChoisirColonnes = new Guna2Button
            {
                Size = new Size(63, 36),
                Margin = new Padding(0, 0, 5, 0),
                FillColor = Color.FromArgb(37, 99, 235),
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.White,
                Text = "Coll"
            };
            btnChoisirColonnes.Click += BtnChoisirColonnes_Click;

            // Bouton Impression générale
            btnImprimer = new Guna2Button
            {
                Size = new Size(38, 36),
                Margin = new Padding(0, 0, 5, 0),
                FillColor = Color.FromArgb(37, 99, 235),
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.White,
                Image = ChargerImageLocale("impriment_icon.png"),
                ImageSize = new Size(20, 20),
                ImageAlign = HorizontalAlignment.Center
            };
            btnImprimer.Click += BtnImprimer_Click;

            // Bouton Ajouter une marque
            btnNouvelleMarque = new Guna2Button
            {
                Size = new Size(191, 36),
                Margin = new Padding(0, 0, 0, 0),
                FillColor = Color.FromArgb(37, 99, 235),
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.White,
                Text = "+ Ajouter une marque"
            };
            btnNouvelleMarque.Click += BtnNouvelleMarque_Click;

            rightActionsPanel.Controls.Add(btnChoisirColonnes);
            rightActionsPanel.Controls.Add(btnImprimer);
            rightActionsPanel.Controls.Add(btnNouvelleMarque);

            headerPanel.Controls.Add(filtreTextBox);
            headerPanel.Controls.Add(listeFiltrageComboBox);
            headerPanel.Controls.Add(lblCompteur);
            headerPanel.Controls.Add(rightActionsPanel);

            // 2. DataGridView
            tableMarquesDataGridView = new Guna2DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                AutoGenerateColumns = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RowHeadersVisible = false,
                CellBorderStyle = DataGridViewCellBorderStyle.None
            };

            tableMarquesDataGridView.RowTemplate.Height = 38;

            tableMarquesDataGridView.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.White,
                ForeColor = Color.Black,
                SelectionBackColor = Color.FromArgb(239, 246, 255),
                SelectionForeColor = Color.Black
            };

            tableMarquesDataGridView.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                Alignment = DataGridViewContentAlignment.MiddleLeft,
                BackColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(37, 99, 235),
                SelectionBackColor = Color.White,
                SelectionForeColor = Color.FromArgb(37, 99, 235)
            };
            tableMarquesDataGridView.ColumnHeadersHeight = 40;

            tableMarquesDataGridView.DefaultCellStyle = new DataGridViewCellStyle
            {
                Alignment = DataGridViewContentAlignment.MiddleLeft,
                BackColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.Black,
                SelectionBackColor = Color.FromArgb(239, 246, 255),
                SelectionForeColor = Color.Black
            };

            tableMarquesDataGridView.CellPainting += Dgv_CellPainting;
            tableMarquesDataGridView.CellClick += Dgv_CellClick;
            tableMarquesDataGridView.CellMouseMove += (s, e) => tableMarquesDataGridView.InvalidateCell(e.ColumnIndex, e.RowIndex);
            tableMarquesDataGridView.CellMouseLeave += (s, e) => tableMarquesDataGridView.InvalidateCell(e.ColumnIndex, e.RowIndex);

            Controls.Add(tableMarquesDataGridView);
            Controls.Add(headerPanel);
        }

        public void ChargerListe()
        {
            string sql = @"
                SELECT m.id AS 'ID',
                       m.code AS 'Code',
                       m.designation AS 'Désignation',
                       (SELECT COUNT(*) FROM Modele md WHERE md.marque_id = m.id) AS 'Modèles liés'
                FROM Marque m
                ORDER BY m.designation";

            DataTable dt = DatabaseHelper.ExecuteQuery(sql);
            tableMarquesDataGridView.DataSource = dt;

            if (tableMarquesDataGridView.Columns.Contains("colModifier")) tableMarquesDataGridView.Columns.Remove("colModifier");
            if (tableMarquesDataGridView.Columns.Contains("colSupprimer")) tableMarquesDataGridView.Columns.Remove("colSupprimer");
            if (tableMarquesDataGridView.Columns.Contains("colImprimer")) tableMarquesDataGridView.Columns.Remove("colImprimer");

            tableMarquesDataGridView.Columns.Add(new DataGridViewButtonColumn { Name = "colModifier", HeaderText = "Modifier", Width = 60, FlatStyle = FlatStyle.Flat });
            tableMarquesDataGridView.Columns.Add(new DataGridViewButtonColumn { Name = "colSupprimer", HeaderText = "Supprimer", Width = 60, FlatStyle = FlatStyle.Flat });
            tableMarquesDataGridView.Columns.Add(new DataGridViewButtonColumn { Name = "colImprimer", HeaderText = "Imprimer", Width = 60, FlatStyle = FlatStyle.Flat });

            AppliquerFiltre();
        }

        private void AppliquerFiltre()
        {
            if (tableMarquesDataGridView.DataSource is not DataTable dt) return;

            string recherche = filtreTextBox.Text.Trim().Replace("'", "''");
            DataView vue = dt.DefaultView;

            if (string.IsNullOrEmpty(recherche))
            {
                vue.RowFilter = "";
            }
            else
            {
                int index = listeFiltrageComboBox.SelectedIndex;
                string colonne = (index >= 0 && index < ColonnesFiltrablesMarque.Length) ? ColonnesFiltrablesMarque[index].Colonne : "";

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

            lblCompteur.Text = (vue.Count == dt.Rows.Count)
                ? $"{dt.Rows.Count} marque(s)"
                : $"{vue.Count} affichée(s) sur {dt.Rows.Count}";
        }

        private void BtnNouvelleMarque_Click(object? sender, EventArgs e)
        {
            using (var frm = new FrmAjouterMarque())
            {
                if (frm.ShowDialog(this) == DialogResult.OK)
                {
                    DerniereMarqueModifieeId = frm.MarqueIdResultat;
                    ChargerListe();
                }
            }
        }

        private void BtnChoisirColonnes_Click(object? sender, EventArgs e)
        {
            var menu = new Guna2ContextMenuStrip();
            foreach (DataGridViewColumn col in tableMarquesDataGridView.Columns)
            {
                var item = new ToolStripMenuItem(col.HeaderText) { Checked = col.Visible, CheckOnClick = true };
                item.Click += (s, args) => col.Visible = item.Checked;
                menu.Items.Add(item);
            }
            menu.Show(btnChoisirColonnes, new Point(0, btnChoisirColonnes.Height));
        }

        private void BtnImprimer_Click(object? sender, EventArgs e)
        {
            if (tableMarquesDataGridView.DataSource is not DataTable dt) return;
            DataView vue = dt.DefaultView;

            if (vue.Count == 0)
            {
                MessageBox.Show("Aucune donnée à imprimer.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var html = new StringBuilder();
            html.Append("<html><head><meta charset='utf-8'><style>");
            html.Append("body{font-family:Segoe UI, sans-serif; margin:25px; color:#000;}");
            html.Append("h1{font-size:18px; text-align:center; color:#181e36;}");
            html.Append("table{border-collapse:collapse; width:100%; margin-top:15px;}");
            html.Append("th,td{border:1px solid #cbd5e1; padding:8px; font-size:12px; text-align:left;}");
            html.Append("th{background:#f1f5f9; color:#2563eb;}");
            html.Append("</style></head><body>");
            html.Append("<h1>Liste des Marques</h1><table><tr>");

            foreach (DataGridViewColumn col in tableMarquesDataGridView.Columns)
            {
                if (col.Visible && !col.Name.StartsWith("col"))
                    html.Append($"<th>{WebUtility.HtmlEncode(col.HeaderText)}</th>");
            }
            html.Append("</tr>");

            foreach (DataRowView row in vue)
            {
                html.Append("<tr>");
                foreach (DataGridViewColumn col in tableMarquesDataGridView.Columns)
                {
                    if (col.Visible && !col.Name.StartsWith("col"))
                    {
                        string field = string.IsNullOrEmpty(col.DataPropertyName) ? col.Name : col.DataPropertyName;
                        string val = dt.Columns.Contains(field) ? row[field]?.ToString() ?? "" : "";
                        html.Append($"<td>{WebUtility.HtmlEncode(val)}</td>");
                    }
                }
                html.Append("</tr>");
            }
            html.Append("</table></body></html>");

            string tempFile = Path.Combine(Path.GetTempPath(), $"rapport_marques_{DateTime.Now:yyyyMMdd_HHmmss}.html");
            File.WriteAllText(tempFile, html.ToString());
            Process.Start(new ProcessStartInfo(tempFile) { UseShellExecute = true });
        }

        private Image? ChargerImageLocale(string fileName)
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "image", fileName);
            if (!File.Exists(path)) path = Path.Combine("image", fileName);
            return File.Exists(path) ? Image.FromFile(path) : null;
        }

        private void Dgv_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0) return;
            Point mousePos = tableMarquesDataGridView.PointToClient(Cursor.Position);
            bool isHovered = e.CellBounds.Contains(mousePos);
            bool isClicked = isHovered && (Control.MouseButtons == MouseButtons.Left);

            if (e.ColumnIndex == tableMarquesDataGridView.Columns["colModifier"]?.Index)
                DessinerBouton(e, isHovered, isClicked, Color.FromArgb(240, 253, 244), Color.FromArgb(220, 252, 231), Color.FromArgb(187, 247, 208), Color.FromArgb(134, 239, 172), "pencil_icon.png");
            else if (e.ColumnIndex == tableMarquesDataGridView.Columns["colSupprimer"]?.Index)
                DessinerBouton(e, isHovered, isClicked, Color.FromArgb(254, 242, 242), Color.FromArgb(254, 226, 226), Color.FromArgb(254, 202, 202), Color.FromArgb(252, 165, 165), "delet_icon.png");
            else if (e.ColumnIndex == tableMarquesDataGridView.Columns["colImprimer"]?.Index)
                DessinerBouton(e, isHovered, isClicked, Color.FromArgb(239, 246, 255), Color.FromArgb(219, 234, 254), Color.FromArgb(191, 219, 254), Color.FromArgb(147, 197, 253), "imprimerbleu.png");
        }

        private static void DessinerBouton(DataGridViewCellPaintingEventArgs e, bool isHovered, bool isClicked, Color bg, Color bgHover, Color bgClick, Color border, string iconFile)
        {
            if (e.Graphics == null) return;

            e.PaintBackground(e.CellBounds, true);
            Color cur = isClicked ? bgClick : (isHovered ? bgHover : bg);
            Rectangle rect = new Rectangle(e.CellBounds.Left + 4, e.CellBounds.Top + 4, e.CellBounds.Width - 8, e.CellBounds.Height - 8);
            using (var brush = new SolidBrush(cur)) e.Graphics.FillRectangle(brush, rect);
            using (var pen = new Pen(border)) e.Graphics.DrawRectangle(pen, rect);

            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "image", iconFile);
            if (!File.Exists(path)) path = Path.Combine("image", iconFile);
            if (File.Exists(path))
            {
                using (Image img = Image.FromFile(path))
                {
                    int size = 18;
                    e.Graphics.DrawImage(img, new Rectangle(rect.Left + (rect.Width - size) / 2, rect.Top + (rect.Height - size) / 2, size, size));
                }
            }
            e.Handled = true;
        }

        private void Dgv_CellClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            string colName = tableMarquesDataGridView.Columns[e.ColumnIndex].Name;
            int id = Convert.ToInt32(tableMarquesDataGridView.Rows[e.RowIndex].Cells["ID"].Value);

            if (colName == "colModifier")
            {
                using (var frm = new FrmAjouterMarque(id))
                {
                    if (frm.ShowDialog(this) == DialogResult.OK)
                    {
                        DerniereMarqueModifieeId = id;
                        ChargerListe();
                        _mainForm?.ChargerEquipements();
                    }
                }
            }
            else if (colName == "colSupprimer")
            {
                int nbModeles = Convert.ToInt32(tableMarquesDataGridView.Rows[e.RowIndex].Cells["Modèles liés"].Value);
                string designation = tableMarquesDataGridView.Rows[e.RowIndex].Cells["Désignation"].Value?.ToString() ?? "";

                if (nbModeles > 0)
                {
                    MessageBox.Show(
                        $"Impossible de supprimer '{designation}' : {nbModeles} modèle(s) utilisent encore cette marque.\n\n" +
                        "Modifiez ou supprimez d'abord ces modèles.",
                        "Suppression refusée", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var confirm = MessageBox.Show($"Supprimer définitivement la marque '{designation}' ?",
                    "Confirmer la suppression", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (confirm != DialogResult.Yes) return;

                try
                {
                    DatabaseHelper.ExecuteNonQuery("DELETE FROM Marque WHERE id=@id", new SqliteParameter("@id", id));
                    ChargerListe();
                }
                catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
                {
                    MessageBox.Show("Suppression refusée par la base de données : cette marque est encore utilisée.",
                        "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else if (colName == "colImprimer")
            {
                ImprimerFicheIndividuelle(tableMarquesDataGridView.Rows[e.RowIndex]);
            }
        }

        private void ImprimerFicheIndividuelle(DataGridViewRow row)
        {
            var html = new StringBuilder();
            html.Append("<html><head><meta charset='utf-8'><style>");
            html.Append("body{font-family:Segoe UI, sans-serif; margin:30px; color:#000;}");
            html.Append(".title{text-align:center; font-weight:bold; font-size:16px; margin-bottom:20px;}");
            html.Append("table{border-collapse:collapse; width:100%;}");
            html.Append("th,td{border:1px solid #cbd5e1; padding:8px 12px; text-align:left;}");
            html.Append("th{background:#f1f5f9; width:30%; color:#2563eb;}");
            html.Append("</style></head><body>");
            html.Append("<div class='title'>FICHE DE MARQUE</div><table>");

            foreach (DataGridViewColumn col in tableMarquesDataGridView.Columns)
            {
                if (col.Visible && !col.Name.StartsWith("col"))
                {
                    string val = row.Cells[col.Index].Value?.ToString() ?? "";
                    html.Append($"<tr><th>{WebUtility.HtmlEncode(col.HeaderText)}</th><td>{WebUtility.HtmlEncode(val)}</td></tr>");
                }
            }
            html.Append("</table></body></html>");

            string tempFile = Path.Combine(Path.GetTempPath(), $"fiche_marque_{row.Cells["ID"].Value}.html");
            File.WriteAllText(tempFile, html.ToString());
            Process.Start(new ProcessStartInfo(tempFile) { UseShellExecute = true });
        }

        private void ChargerIconeBouton(Guna2Button btn, string iconName)
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "image", iconName);
            if (!File.Exists(path)) path = Path.Combine("image", iconName);
            if (File.Exists(path))
            {
                btn.Image = Image.FromFile(path);
                btn.ImageSize = new Size(18, 18);
            }
        }
    }
}