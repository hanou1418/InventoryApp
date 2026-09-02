#nullable enable
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Guna.UI2.WinForms;
using InventoryApp.Data;
using Microsoft.Data.Sqlite;

namespace InventoryApp
{
    /// <summary>
    /// Fenêtre de gestion complète des Modèles (liste + CRUD).
    /// La suppression est protégée : impossible si des équipements
    /// utilisent encore ce modèle (vérification préalable + filet de
    /// sécurité via la contrainte FOREIGN KEY ON DELETE RESTRICT).
    /// </summary>
    public class FrmGererModeles : Form
    {
        private readonly Form1? _mainForm;

        // Si renseigné, sélectionne automatiquement ce modèle dans le
        // combo appelant après fermeture (pratique depuis FrmAjouterArticle).
        public int? DernierModeleModifieId { get; private set; } = null;

        private Guna2BorderlessForm borderlessForm = null!;
        private Guna2Panel pnlHeader = null!;
        private Label lblHeaderTitle = null!;
        private Guna2ControlBox btnCloseHeader = null!;

        private Guna2Button btnNouveau = null!;
        private Guna2Button btnGererCategories = null!;
        private Guna2Button btnGererMarques = null!;
        private Guna2DataGridView dgv = null!;

        public FrmGererModeles(Form1? mainForm)
        {
            _mainForm = mainForm;

            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.FromArgb(248, 250, 252);
            Width = 780;
            Height = 560;

            ConstruireControles();
            Load += (s, e) => ChargerListe();
        }

        private void ConstruireControles()
        {
            borderlessForm = new Guna2BorderlessForm { ContainerControl = this, BorderRadius = 14, DragForm = true, HasFormShadow = true };

            pnlHeader = new Guna2Panel { Dock = DockStyle.Top, Height = 50, FillColor = Color.FromArgb(30, 41, 59) };
            lblHeaderTitle = new Label { Text = "Gérer les modèles", Font = new Font("Segoe UI", 11F, FontStyle.Bold), ForeColor = Color.White, BackColor = Color.Transparent, AutoSize = true, Left = 20, Top = 14 };
            btnCloseHeader = new Guna2ControlBox
            {
                ControlBoxType = Guna.UI2.WinForms.Enums.ControlBoxType.CloseBox,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Left = Width - 40,
                Top = 10,
                Size = new Size(30, 30),
                FillColor = Color.Transparent,
                IconColor = Color.White,
                BorderRadius = 6
            };
            pnlHeader.Controls.Add(lblHeaderTitle);
            pnlHeader.Controls.Add(btnCloseHeader);

            btnNouveau = new Guna2Button { Text = "+ Nouveau modèle", Left = 20, Top = 65, Width = 160, Height = 36, BorderRadius = 6, FillColor = Color.FromArgb(59, 130, 246), ForeColor = Color.White };
            btnNouveau.Click += (s, e) =>
            {
                using (var frm = new FrmAjouterModele())
                {
                    if (frm.ShowDialog(this) == DialogResult.OK)
                    {
                        DernierModeleModifieId = frm.ModeleIdResultat;
                        ChargerListe();
                    }
                }
            };

            btnGererCategories = new Guna2Button { Text = "Gérer catégories", Left = 190, Top = 65, Width = 150, Height = 36, BorderRadius = 6, FillColor = Color.FromArgb(100, 116, 139), ForeColor = Color.White };
            btnGererCategories.Click += (s, e) =>
            {
                using (var frm = new FrmGererCategories(_mainForm)) frm.ShowDialog(this);
                ChargerListe();
            };

            btnGererMarques = new Guna2Button { Text = "Gérer marques", Left = 350, Top = 65, Width = 150, Height = 36, BorderRadius = 6, FillColor = Color.FromArgb(100, 116, 139), ForeColor = Color.White };
            btnGererMarques.Click += (s, e) =>
            {
                using (var frm = new FrmGererMarques(_mainForm)) frm.ShowDialog(this);
                ChargerListe();
            };

            dgv = new Guna2DataGridView
            {
                Left = 20,
                Top = 115,
                Width = 730,
                Height = 390,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                AutoGenerateColumns = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RowTemplate = { Height = 38 }
            };
            dgv.CellPainting += Dgv_CellPainting;
            dgv.CellClick += Dgv_CellClick;
            dgv.CellMouseMove += (s, e) => dgv.InvalidateCell(e.ColumnIndex, e.RowIndex);
            dgv.CellMouseLeave += (s, e) => dgv.InvalidateCell(e.ColumnIndex, e.RowIndex);

            Controls.Add(pnlHeader);
            Controls.Add(btnNouveau);
            Controls.Add(btnGererCategories);
            Controls.Add(btnGererMarques);
            Controls.Add(dgv);
        }

        private void ChargerListe()
        {
            string sql = @"
                SELECT md.id AS 'ID', md.reference AS 'Référence', md.designation AS 'Désignation',
                       COALESCE(c.designation,'—') AS 'Catégorie', COALESCE(mq.designation,'—') AS 'Marque',
                       (SELECT COUNT(*) FROM Equipement e WHERE e.modele_id = md.id) AS 'Équipements liés'
                FROM Modele md
                LEFT JOIN Categorie c ON md.categorie_id = c.id
                LEFT JOIN Marque mq ON md.marque_id = mq.id
                ORDER BY md.designation";
            dgv.DataSource = DatabaseHelper.ExecuteQuery(sql);

            if (dgv.Columns.Contains("colModifier")) dgv.Columns.Remove("colModifier");
            if (dgv.Columns.Contains("colSupprimer")) dgv.Columns.Remove("colSupprimer");
            dgv.Columns.Add(new DataGridViewButtonColumn { Name = "colModifier", HeaderText = "Modifier", Width = 60, FlatStyle = FlatStyle.Flat });
            dgv.Columns.Add(new DataGridViewButtonColumn { Name = "colSupprimer", HeaderText = "Supprimer", Width = 60, FlatStyle = FlatStyle.Flat });
        }

        private void Dgv_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0) return;
            Point mousePos = dgv.PointToClient(Cursor.Position);
            bool isHovered = e.CellBounds.Contains(mousePos);
            bool isClicked = isHovered && (Control.MouseButtons == MouseButtons.Left);

            if (e.ColumnIndex == dgv.Columns["colModifier"]?.Index)
                DessinerBouton(e, isHovered, isClicked, Color.FromArgb(240, 253, 244), Color.FromArgb(220, 252, 231), Color.FromArgb(187, 247, 208), Color.FromArgb(134, 239, 172), "pencil_icon.png");
            else if (e.ColumnIndex == dgv.Columns["colSupprimer"]?.Index)
                DessinerBouton(e, isHovered, isClicked, Color.FromArgb(254, 242, 242), Color.FromArgb(254, 226, 226), Color.FromArgb(254, 202, 202), Color.FromArgb(252, 165, 165), "delet_icon.png");
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
            string colName = dgv.Columns[e.ColumnIndex].Name;
            int id = Convert.ToInt32(dgv.Rows[e.RowIndex].Cells["ID"].Value);

            if (colName == "colModifier")
            {
                using (var frm = new FrmAjouterModele(id))
                {
                    if (frm.ShowDialog(this) == DialogResult.OK)
                    {
                        DernierModeleModifieId = id;
                        ChargerListe();
                        _mainForm?.ChargerEquipements();
                    }
                }
            }
            else if (colName == "colSupprimer")
            {
                int nbEquip = Convert.ToInt32(dgv.Rows[e.RowIndex].Cells["Équipements liés"].Value);
                string designation = dgv.Rows[e.RowIndex].Cells["Désignation"].Value?.ToString() ?? "";

                if (nbEquip > 0)
                {
                    MessageBox.Show(
                        $"Impossible de supprimer '{designation}' : {nbEquip} équipement(s) utilisent encore ce modèle.\n\n" +
                        "Modifiez ou supprimez d'abord ces équipements dans la liste du stock.",
                        "Suppression refusée", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var confirm = MessageBox.Show($"Supprimer définitivement le modèle '{designation}' ?",
                    "Confirmer la suppression", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (confirm != DialogResult.Yes) return;

                try
                {
                    DatabaseHelper.ExecuteNonQuery("DELETE FROM Modele WHERE id=@id", new SqliteParameter("@id", id));
                    ChargerListe();
                }
                catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
                {
                    MessageBox.Show("Suppression refusée par la base de données : ce modèle est encore utilisé.",
                        "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}