#nullable enable
using System;
using System.Drawing;
using System.Windows.Forms;
using Guna.UI2.WinForms;
using InventoryApp.Data;
using Microsoft.Data.Sqlite;

namespace InventoryApp
{
    public class FrmAjouterEmploye : Form
    {
        // Palette de couleurs demandée
        private readonly Color _primaryBlue = Color.FromArgb(37, 99, 235);
        private readonly Color _darkNavy = Color.FromArgb(24, 30, 54);
        private readonly Color _lightGray = Color.FromArgb(240, 242, 245);

        public int? EmployeIdResultat { get; private set; } = null;

        private readonly int? _employeIdEnEdition;
        private bool EnModeEdition => _employeIdEnEdition.HasValue;

        private Guna2TextBox txtNom = null!;
        private Guna2TextBox txtPrenom = null!;
        private Guna2TextBox txtDepartement = null!;
        private Guna2TextBox txtFonction = null!;
        private Guna2Button btnEnregistrer = null!;
        private Guna2Button btnAnnuler = null!;

        public FrmAjouterEmploye(int? employeIdEnEdition = null)
        {
            _employeIdEnEdition = employeIdEnEdition;

            Text = EnModeEdition ? "Modifier l'employé" : "Nouvel employé";
            Size = new Size(460, 410);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.None; // Fenêtre épurée moderne
            BackColor = Color.White;

            ConstruireInterface();

            if (EnModeEdition) Load += (s, e) => ChargerDonnees();
        }

        private void ConstruireInterface()
        {
            // Panel En-tête
            var panelHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = _darkNavy
            };

            var lblTitre = new Label
            {
                Text = EnModeEdition ? "MODIFIER L'EMPLOYÉ" : "NOUVEL EMPLOYÉ",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            panelHeader.Controls.Add(lblTitre);
            Controls.Add(panelHeader);

            // Container Formulaire
            int y = 80;
            const int marginX = 30;
            const int inputWidthFull = 384;
            const int inputWidthHalf = 186;

            Label CreateLabel(string text, int x, int topWidth)
            {
                var lbl = new Label
                {
                    Text = text,
                    Left = x,
                    Top = y,
                    Width = topWidth,
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                    ForeColor = _darkNavy
                };
                Controls.Add(lbl);
                return lbl;
            }

            Guna2TextBox CreateTextBox(int x, int width, string placeholder = "")
            {
                var txt = new Guna2TextBox
                {
                    Left = x,
                    Top = y + 20,
                    Width = width,
                    Height = 38,
                    BorderRadius = 6,
                    BorderColor = Color.LightGray,
                    FocusedState = { BorderColor = _primaryBlue },
                    Font = new Font("Segoe UI", 9.5F),
                    ForeColor = _darkNavy,
                    PlaceholderText = placeholder
                };
                Controls.Add(txt);
                return txt;
            }

            // Ligne 1 : Nom et Prénom côte à côte
            CreateLabel("Nom *", marginX, inputWidthHalf);
            CreateLabel("Prénom *", marginX + inputWidthHalf + 12, inputWidthHalf);
            txtNom = CreateTextBox(marginX, inputWidthHalf, "ex: Dupont");
            txtPrenom = CreateTextBox(marginX + inputWidthHalf + 12, inputWidthHalf, "ex: Jean");
            y += 68;

            // Ligne 2 : Département
            CreateLabel("Département", marginX, inputWidthFull);
            txtDepartement = CreateTextBox(marginX, inputWidthFull, "ex: Informatique");
            y += 68;

            // Ligne 3 : Fonction
            CreateLabel("Fonction", marginX, inputWidthFull);
            txtFonction = CreateTextBox(marginX, inputWidthFull, "ex: Développeur");
            y += 80;

            // Boutons d'action
            btnEnregistrer = new Guna2Button
            {
                Text = "Enregistrer",
                Left = marginX + 194,
                Top = y,
                Width = 190,
                Height = 42,
                BorderRadius = 6,
                FillColor = _primaryBlue,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnEnregistrer.Click += BtnEnregistrer_Click;

            btnAnnuler = new Guna2Button
            {
                Text = "Annuler",
                Left = marginX,
                Top = y,
                Width = 180,
                Height = 42,
                BorderRadius = 6,
                FillColor = _lightGray,
                ForeColor = _darkNavy,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnAnnuler.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            Controls.Add(btnEnregistrer);
            Controls.Add(btnAnnuler);
        }

        private void ChargerDonnees()
        {
            var t = DatabaseHelper.ExecuteQuery(
                "SELECT nom, prenom, departement, function FROM Employe WHERE id = @id",
                new SqliteParameter("@id", _employeIdEnEdition!.Value));

            if (t.Rows.Count == 0)
            {
                MessageBox.Show("Employé introuvable.", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close();
                return;
            }

            var row = t.Rows[0];
            txtNom.Text = row["nom"]?.ToString() ?? "";
            txtPrenom.Text = row["prenom"]?.ToString() ?? "";
            txtDepartement.Text = row["departement"]?.ToString() ?? "";
            txtFonction.Text = row["function"]?.ToString() ?? "";
        }

        private void BtnEnregistrer_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtNom.Text) || string.IsNullOrWhiteSpace(txtPrenom.Text))
            {
                MessageBox.Show("Le nom et le prénom sont obligatoires.", "Champs manquants", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                using (var cmd = conn.CreateCommand())
                {
                    if (EnModeEdition)
                    {
                        cmd.CommandText = @"
                            UPDATE Employe
                            SET nom=@nom, prenom=@prenom, departement=@dep, function=@fct
                            WHERE id=@id";
                        cmd.Parameters.AddWithValue("@id", _employeIdEnEdition!.Value);
                        EployeIdResultatSet(_employeIdEnEdition!.Value);
                    }
                    else
                    {
                        cmd.CommandText = @"
                            INSERT INTO Employe (nom, prenom, departement, function)
                            VALUES (@nom, @prenom, @dep, @fct);
                            SELECT last_insert_rowid();";
                    }

                    cmd.Parameters.AddWithValue("@nom", txtNom.Text.Trim());
                    cmd.Parameters.AddWithValue("@prenom", txtPrenom.Text.Trim());
                    cmd.Parameters.AddWithValue("@dep",
                        string.IsNullOrWhiteSpace(txtDepartement.Text) ? (object)DBNull.Value : txtDepartement.Text.Trim());
                    cmd.Parameters.AddWithValue("@fct",
                        string.IsNullOrWhiteSpace(txtFonction.Text) ? (object)DBNull.Value : txtFonction.Text.Trim());

                    if (!EnModeEdition)
                    {
                        var newId = Convert.ToInt32(cmd.ExecuteScalar());
                        EmployeIdResultat = newId;
                    }
                    else
                    {
                        cmd.ExecuteNonQuery();
                        EmployeIdResultat = _employeIdEnEdition;
                    }
                }

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur : " + ex.Message, "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void EployeIdResultatSet(int id) { EmployeIdResultat = id; }
    }
}