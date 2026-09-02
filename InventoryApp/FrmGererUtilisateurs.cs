#nullable enable
using System;
using System.Drawing;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using Guna.UI2.WinForms;
using InventoryApp.Data;
using Microsoft.Data.Sqlite;

namespace InventoryApp
{
    /// <summary>
    /// Popup de gestion des utilisateurs : liste avec Modifier/Supprimer,
    /// ajout d'un nouvel utilisateur, changement de mot de passe.
    /// Accessible depuis Form1 (menu ou bouton "Mon compte").
    /// </summary>
    public class FrmGererUtilisateurs : Form
    {
        private Guna2BorderlessForm borderlessForm = null!;
        private Guna2Panel pnlHeader = null!;
        private Label lblHeaderTitle = null!;
        private Guna2ControlBox btnCloseHeader = null!;

        private Guna2Button btnNouveau = null!;
        private Guna2DataGridView dgv = null!;

        public FrmGererUtilisateurs()
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.FromArgb(248, 250, 252);
            Width = 620;
            Height = 480;

            ConstruireControles();
            Load += (s, e) => ChargerListe();
        }

        private void ConstruireControles()
        {
            borderlessForm = new Guna2BorderlessForm { ContainerControl = this, BorderRadius = 14, DragForm = true, HasFormShadow = true };

            pnlHeader = new Guna2Panel { Dock = DockStyle.Top, Height = 50, FillColor = Color.FromArgb(30, 41, 59) };
            lblHeaderTitle = new Label { Text = "Gérer les utilisateurs", Font = new Font("Segoe UI", 11F, FontStyle.Bold), ForeColor = Color.White, BackColor = Color.Transparent, AutoSize = true, Left = 20, Top = 14 };
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

            btnNouveau = new Guna2Button { Text = "+ Nouvel utilisateur", Left = 20, Top = 65, Width = 180, Height = 36, BorderRadius = 6, FillColor = Color.FromArgb(59, 130, 246), ForeColor = Color.White };
            btnNouveau.Click += (s, e) =>
            {
                using (var frm = new FrmAjouterUtilisateur()) { if (frm.ShowDialog(this) == DialogResult.OK) ChargerListe(); }
            };

            dgv = new Guna2DataGridView
            {
                Left = 20,
                Top = 115,
                Width = 570,
                Height = 310,
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

            Controls.Add(pnlHeader); Controls.Add(btnNouveau); Controls.Add(dgv);
        }

        private void ChargerListe()
        {
            var t = DatabaseHelper.ExecuteQuery("SELECT id AS 'ID', login AS 'Login', nom_affichage AS 'Nom affiché', CASE actif WHEN 1 THEN 'Actif' ELSE 'Inactif' END AS 'Statut' FROM Utilisateur ORDER BY login");
            dgv.DataSource = t;

            if (dgv.Columns.Contains("colModifier")) dgv.Columns.Remove("colModifier");
            if (dgv.Columns.Contains("colSupprimer")) dgv.Columns.Remove("colSupprimer");
            dgv.Columns.Add(new DataGridViewButtonColumn { Name = "colModifier", HeaderText = "Modifier", Width = 65, FlatStyle = FlatStyle.Flat });
            dgv.Columns.Add(new DataGridViewButtonColumn { Name = "colSupprimer", HeaderText = "Supprimer", Width = 65, FlatStyle = FlatStyle.Flat });
        }

        private void Dgv_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0) return;
            Point mouse = dgv.PointToClient(Cursor.Position);
            bool hover = e.CellBounds.Contains(mouse);
            bool click = hover && Control.MouseButtons == MouseButtons.Left;

            if (e.ColumnIndex == dgv.Columns["colModifier"]?.Index)
                DessinerBouton(e, hover, click, Color.FromArgb(240, 253, 244), Color.FromArgb(220, 252, 231), Color.FromArgb(187, 247, 208), Color.FromArgb(134, 239, 172), "pencil_icon.png");
            else if (e.ColumnIndex == dgv.Columns["colSupprimer"]?.Index)
                DessinerBouton(e, hover, click, Color.FromArgb(254, 242, 242), Color.FromArgb(254, 226, 226), Color.FromArgb(254, 202, 202), Color.FromArgb(252, 165, 165), "delet_icon.png");
        }

        private void DessinerBouton(DataGridViewCellPaintingEventArgs e, bool hover, bool click, Color bg, Color bgHover, Color bgClick, Color border, string iconFile)
        {
            e.PaintBackground(e.CellBounds, true);
            Color cur = click ? bgClick : (hover ? bgHover : bg);
            Rectangle rect = new(e.CellBounds.Left + 4, e.CellBounds.Top + 4, e.CellBounds.Width - 8, e.CellBounds.Height - 8);
            using (var b = new SolidBrush(cur)) e.Graphics.FillRectangle(b, rect);
            using (var p = new Pen(border)) e.Graphics.DrawRectangle(p, rect);
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "image", iconFile);
            if (!File.Exists(path)) path = Path.Combine("image", iconFile);
            if (File.Exists(path))
                using (Image img = Image.FromFile(path))
                    e.Graphics.DrawImage(img, new Rectangle(rect.Left + (rect.Width - 18) / 2, rect.Top + (rect.Height - 18) / 2, 18, 18));
            e.Handled = true;
        }

        private void Dgv_CellClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            string colName = dgv.Columns[e.ColumnIndex].Name ?? "";
            int id = Convert.ToInt32(dgv.Rows[e.RowIndex].Cells["ID"].Value);
            string login = dgv.Rows[e.RowIndex].Cells["Login"].Value?.ToString() ?? "";

            if (colName == "colModifier")
            {
                using (var frm = new FrmAjouterUtilisateur(id)) { if (frm.ShowDialog(this) == DialogResult.OK) ChargerListe(); }
            }
            else if (colName == "colSupprimer")
            {
                // Empêcher la suppression de son propre compte
                if (id == SessionUtilisateur.Id)
                {
                    MessageBox.Show("Vous ne pouvez pas supprimer votre propre compte.", "Action refusée", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Interdire la suppression si c'est le seul compte actif
                int nbActifs = Convert.ToInt32(DatabaseHelper.ExecuteQuery("SELECT COUNT(*) AS n FROM Utilisateur WHERE actif=1").Rows[0]["n"]);
                if (nbActifs <= 1)
                {
                    MessageBox.Show("Impossible de supprimer le dernier utilisateur actif.\nDésactivez-le plutôt ou créez un autre compte d'abord.", "Action refusée", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (MessageBox.Show($"Supprimer définitivement l'utilisateur '{login}' ?", "Confirmer", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    DatabaseHelper.ExecuteNonQuery("DELETE FROM Utilisateur WHERE id=@id", new SqliteParameter("@id", id));
                    ChargerListe();
                }
            }
        }
    }

    /// <summary>
    /// Popup d'ajout/modification d'un utilisateur.
    /// En mode modification, laisser les champs mdp vides = ne pas changer.
    /// </summary>
    public class FrmAjouterUtilisateur : Form
    {
        private readonly int? _idEnEdition;
        private bool EnModeEdition => _idEnEdition.HasValue;

        private Guna2BorderlessForm borderlessForm = null!;
        private Guna2Panel pnlHeader = null!;
        private Label lblHeaderTitle = null!;
        private Guna2ControlBox btnCloseHeader = null!;

        private Guna2TextBox txtLogin = null!;
        private Guna2TextBox txtNomAffichage = null!;
        private Guna2TextBox txtMdp = null!;
        private Guna2TextBox txtMdpConfirm = null!;
        private Guna2ComboBox cmbActif = null!;
        private Guna2Button btnEnregistrer = null!;
        private Guna2Button btnAnnuler = null!;

        public FrmAjouterUtilisateur(int? idEnEdition = null)
        {
            _idEnEdition = idEnEdition;
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.FromArgb(248, 250, 252);
            Width = 420;
            Height = 430;
            ConstruireControles();
            if (EnModeEdition) Load += (s, e) => ChargerDonnees();
        }

        private void ConstruireControles()
        {
            borderlessForm = new Guna2BorderlessForm { ContainerControl = this, BorderRadius = 14, DragForm = true, HasFormShadow = true };
            pnlHeader = new Guna2Panel { Dock = DockStyle.Top, Height = 50, FillColor = Color.FromArgb(30, 41, 59) };
            lblHeaderTitle = new Label { Text = EnModeEdition ? "Modifier l'utilisateur" : "Nouvel utilisateur", Font = new Font("Segoe UI", 11F, FontStyle.Bold), ForeColor = Color.White, BackColor = Color.Transparent, AutoSize = true, Left = 20, Top = 14 };
            btnCloseHeader = new Guna2ControlBox { ControlBoxType = Guna.UI2.WinForms.Enums.ControlBoxType.CloseBox, Anchor = AnchorStyles.Top | AnchorStyles.Right, Left = Width - 40, Top = 10, Size = new Size(30, 30), FillColor = Color.Transparent, IconColor = Color.White, BorderRadius = 6 };
            pnlHeader.Controls.Add(lblHeaderTitle); pnlHeader.Controls.Add(btnCloseHeader);

            int y = 62;
            Label L(string t) { var l = new Label { Text = t, Left = 20, Top = y, Width = 370, ForeColor = Color.DimGray }; Controls.Add(l); y += 20; return l; }
            Guna2TextBox T(string ph, bool mdp = false) { var tb = new Guna2TextBox { Left = 20, Top = y, Width = 370, Height = 36, BorderRadius = 6, PlaceholderText = ph, UseSystemPasswordChar = mdp }; Controls.Add(tb); y += 48; return tb; }

            L("Identifiant (login) *"); txtLogin = T("Ex : jean.dupont");
            L("Nom affiché"); txtNomAffichage = T("Ex : Jean Dupont");
            L(EnModeEdition ? "Nouveau mot de passe (vide = inchangé)" : "Mot de passe *"); txtMdp = T("••••••••", true);
            L("Confirmer le mot de passe"); txtMdpConfirm = T("••••••••", true);

            L("Statut");
            cmbActif = new Guna2ComboBox { Left = 20, Top = y, Width = 370, Height = 36, BorderRadius = 6, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbActif.Items.AddRange(new object[] { "Actif", "Inactif" });
            cmbActif.SelectedIndex = 0;
            Controls.Add(cmbActif); y += 55;

            btnEnregistrer = new Guna2Button { Text = "Enregistrer", Left = 180, Top = y, Width = 110, Height = 36, BorderRadius = 6, FillColor = Color.FromArgb(59, 130, 246), ForeColor = Color.White };
            btnAnnuler = new Guna2Button { Text = "Annuler", Left = 300, Top = y, Width = 90, Height = 36, BorderRadius = 6, FillColor = Color.Gray, ForeColor = Color.White };
            btnEnregistrer.Click += BtnEnregistrer_Click;
            btnAnnuler.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            Controls.Add(pnlHeader); Controls.Add(btnEnregistrer); Controls.Add(btnAnnuler);
            Height = y + 80;
        }

        private void ChargerDonnees()
        {
            var t = DatabaseHelper.ExecuteQuery("SELECT login, nom_affichage, actif FROM Utilisateur WHERE id=@id", new SqliteParameter("@id", _idEnEdition!.Value));
            if (t.Rows.Count == 0) { Close(); return; }
            txtLogin.Text = t.Rows[0]["login"]?.ToString() ?? "";
            txtNomAffichage.Text = t.Rows[0]["nom_affichage"]?.ToString() ?? "";
            cmbActif.SelectedIndex = Convert.ToInt32(t.Rows[0]["actif"]) == 1 ? 0 : 1;
        }

        private static string HashSha256(string texte)
        {
            using var sha = SHA256.Create();
            return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(texte))).ToLower();
        }

        private void BtnEnregistrer_Click(object? sender, EventArgs e)
        {
            string login = txtLogin.Text.Trim();
            string nom = txtNomAffichage.Text.Trim();
            string mdp = txtMdp.Text;
            string mdpConf = txtMdpConfirm.Text;
            bool actif = cmbActif.SelectedIndex == 0;

            if (string.IsNullOrWhiteSpace(login))
            {
                MessageBox.Show("L'identifiant est obligatoire.", "Champ manquant", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            bool changerMdp = !string.IsNullOrEmpty(mdp) || !EnModeEdition;
            if (changerMdp)
            {
                if (string.IsNullOrEmpty(mdp))
                {
                    MessageBox.Show("Le mot de passe est obligatoire pour un nouvel utilisateur.", "Champ manquant", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (mdp.Length < 6)
                {
                    MessageBox.Show("Le mot de passe doit contenir au moins 6 caractères.", "Trop court", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (mdp != mdpConf)
                {
                    MessageBox.Show("Les deux mots de passe ne correspondent pas.", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                using (var cmd = conn.CreateCommand())
                {
                    if (EnModeEdition)
                    {
                        if (changerMdp)
                        {
                            cmd.CommandText = "UPDATE Utilisateur SET login=@l, nom_affichage=@n, mot_de_passe_hash=@h, actif=@a WHERE id=@id";
                            cmd.Parameters.AddWithValue("@h", HashSha256(mdp));
                        }
                        else
                        {
                            cmd.CommandText = "UPDATE Utilisateur SET login=@l, nom_affichage=@n, actif=@a WHERE id=@id";
                        }
                        cmd.Parameters.AddWithValue("@id", _idEnEdition!.Value);
                    }
                    else
                    {
                        cmd.CommandText = "INSERT INTO Utilisateur (login, nom_affichage, mot_de_passe_hash, actif) VALUES (@l, @n, @h, @a)";
                        cmd.Parameters.AddWithValue("@h", HashSha256(mdp));
                    }
                    cmd.Parameters.AddWithValue("@l", login);
                    cmd.Parameters.AddWithValue("@n", string.IsNullOrWhiteSpace(nom) ? (object)DBNull.Value : nom);
                    cmd.Parameters.AddWithValue("@a", actif ? 1 : 0);
                    cmd.ExecuteNonQuery();
                }
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
            {
                MessageBox.Show("Cet identifiant existe déjà.", "Doublon", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}