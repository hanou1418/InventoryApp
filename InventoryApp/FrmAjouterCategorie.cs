#nullable enable
using System;
using System.Drawing;
using System.Windows.Forms;
using Guna.UI2.WinForms;
using InventoryApp.Data;
using Microsoft.Data.Sqlite;

namespace InventoryApp
{
    /// <summary>
    /// Popup Guna.UI2 (meme style que FrmAjouterArticle) pour ajouter
    /// ou modifier UNE Categorie. Reutilise par FrmAjouterArticle
    /// (creation rapide) et par FrmGererCategories (gestion complete).
    /// </summary>
    public class FrmAjouterCategorie : Form
    {
        public int? CategorieIdResultat { get; private set; } = null;

        private readonly int? _categorieIdEnEdition;
        private bool EnModeEdition => _categorieIdEnEdition.HasValue;

        private Guna2BorderlessForm borderlessForm = null!;
        private Guna2Panel pnlHeader = null!;
        private Label lblHeaderTitle = null!;
        private Guna2ControlBox btnCloseHeader = null!;

        private Guna2TextBox txtCode = null!;
        private Guna2TextBox txtDesignation = null!;
        private Guna2Button btnEnregistrer = null!;
        private Guna2Button btnAnnuler = null!;

        public FrmAjouterCategorie(int? categorieIdEnEdition = null)
        {
            _categorieIdEnEdition = categorieIdEnEdition;

            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.FromArgb(248, 250, 252);
            Width = 400;
            Height = 250;

            ConstruireControles();
            if (EnModeEdition) Load += (s, e) => ChargerDonnees();
        }

        private void ConstruireControles()
        {
            borderlessForm = new Guna2BorderlessForm { ContainerControl = this, BorderRadius = 14, DragForm = true, HasFormShadow = true };

            pnlHeader = new Guna2Panel { Dock = DockStyle.Top, Height = 50, FillColor = Color.FromArgb(30, 41, 59) };
            lblHeaderTitle = new Label
            {
                Text = EnModeEdition ? "Modifier la catégorie" : "Nouvelle catégorie",
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                AutoSize = true,
                Left = 20,
                Top = 14
            };
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

            var lblCode = new Label { Text = "Code (2 à 4 lettres) *", Left = 20, Top = 65, Width = 300, ForeColor = Color.DimGray };
            txtCode = new Guna2TextBox { Left = 20, Top = 88, Width = 340, Height = 36, BorderRadius = 6, PlaceholderText = "Ex : IMP, ORD, MOB" };

            var lblDesignation = new Label { Text = "Désignation *", Left = 20, Top = 132, Width = 300, ForeColor = Color.DimGray };
            txtDesignation = new Guna2TextBox { Left = 20, Top = 155, Width = 340, Height = 36, BorderRadius = 6, PlaceholderText = "Ex : Imprimante" };

            btnEnregistrer = new Guna2Button { Text = "Enregistrer", Left = 170, Top = 200, Width = 100, Height = 36, BorderRadius = 6, FillColor = Color.FromArgb(59, 130, 246), ForeColor = Color.White };
            btnAnnuler = new Guna2Button { Text = "Annuler", Left = 280, Top = 200, Width = 80, Height = 36, BorderRadius = 6, FillColor = Color.Gray, ForeColor = Color.White };
            btnEnregistrer.Click += BtnEnregistrer_Click;
            btnAnnuler.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            Controls.Add(pnlHeader);
            Controls.Add(lblCode);
            Controls.Add(txtCode);
            Controls.Add(lblDesignation);
            Controls.Add(txtDesignation);
            Controls.Add(btnEnregistrer);
            Controls.Add(btnAnnuler);
        }

        private void ChargerDonnees()
        {
            var t = DatabaseHelper.ExecuteQuery("SELECT code, designation FROM Categorie WHERE id=@id",
                new SqliteParameter("@id", _categorieIdEnEdition!.Value));
            if (t.Rows.Count == 0) { Close(); return; }
            txtCode.Text = t.Rows[0]["code"]?.ToString() ?? "";
            txtDesignation.Text = t.Rows[0]["designation"]?.ToString() ?? "";
        }

        private void BtnEnregistrer_Click(object? sender, EventArgs e)
        {
            string code = txtCode.Text.Trim().ToUpper();
            string desig = txtDesignation.Text.Trim();

            if (code.Length < 2 || code.Length > 4)
            {
                MessageBox.Show("Le code doit contenir entre 2 et 4 lettres.", "Code invalide", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(desig))
            {
                MessageBox.Show("La désignation est obligatoire.", "Champ manquant", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                using (var cmd = conn.CreateCommand())
                {
                    if (EnModeEdition)
                    {
                        cmd.CommandText = "UPDATE Categorie SET code=@code, designation=@desig WHERE id=@id";
                        cmd.Parameters.AddWithValue("@id", _categorieIdEnEdition!.Value);
                        cmd.Parameters.AddWithValue("@code", code);
                        cmd.Parameters.AddWithValue("@desig", desig);
                        cmd.ExecuteNonQuery();
                        CategorieIdResultat = _categorieIdEnEdition;
                    }
                    else
                    {
                        cmd.CommandText = "INSERT INTO Categorie (code, designation) VALUES (@code, @desig); SELECT last_insert_rowid();";
                        cmd.Parameters.AddWithValue("@code", code);
                        cmd.Parameters.AddWithValue("@desig", desig);
                        CategorieIdResultat = Convert.ToInt32(cmd.ExecuteScalar());
                    }
                }
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
            {
                MessageBox.Show("Ce code de catégorie existe déjà.", "Doublon", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}