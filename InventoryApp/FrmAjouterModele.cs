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
    /// Popup Guna.UI2 pour ajouter ou modifier UN Modele.
    /// - Référence OPTIONNELLE : laissée vide, elle est auto-générée par
    ///   le trigger SQL trg_auto_reference_modele (ex: AUTO-00007).
    /// - Désignation OBLIGATOIRE : c'est le seul champ vraiment requis.
    /// - Catégorie/Marque : optionnelles, avec bouton "+" pour en créer
    ///   une nouvelle sans quitter ce formulaire.
    /// </summary>
    public class FrmAjouterModele : Form
    {
        public int? ModeleIdResultat { get; private set; } = null;

        private readonly int? _modeleIdEnEdition;
        private bool EnModeEdition => _modeleIdEnEdition.HasValue;

        private Guna2BorderlessForm borderlessForm = null!;
        private Guna2Panel pnlHeader = null!;
        private Label lblHeaderTitle = null!;
        private Guna2ControlBox btnCloseHeader = null!;

        private Guna2TextBox txtDesignation = null!;
        private Guna2TextBox txtReference = null!;
        private Guna2ComboBox cmbCategorie = null!;
        private Guna2Button btnNouvelleCategorie = null!;
        private Guna2ComboBox cmbMarque = null!;
        private Guna2Button btnNouvelleMarque = null!;
        private Guna2Button btnEnregistrer = null!;
        private Guna2Button btnAnnuler = null!;

        public FrmAjouterModele(int? modeleIdEnEdition = null)
        {
            _modeleIdEnEdition = modeleIdEnEdition;

            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.FromArgb(248, 250, 252);
            Width = 430;
            Height = 400;

            ConstruireControles();
            Load += (s, e) => { ChargerCategories(); ChargerMarques(); if (EnModeEdition) ChargerDonnees(); };
        }

        private void ConstruireControles()
        {
            borderlessForm = new Guna2BorderlessForm { ContainerControl = this, BorderRadius = 14, DragForm = true, HasFormShadow = true };

            pnlHeader = new Guna2Panel { Dock = DockStyle.Top, Height = 50, FillColor = Color.FromArgb(30, 41, 59) };
            lblHeaderTitle = new Label
            {
                Text = EnModeEdition ? "Modifier le modèle" : "Nouveau modèle",
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

            int y = 65;
            var lblDesig = new Label { Text = "Désignation * (obligatoire)", Left = 20, Top = y, Width = 380, ForeColor = Color.DimGray };
            y += 23;
            txtDesignation = new Guna2TextBox { Left = 20, Top = y, Width = 380, Height = 36, BorderRadius = 6, PlaceholderText = "Ex : HP LaserJet 1020" };
            y += 50;

            var lblRef = new Label { Text = "Référence (optionnel — auto-générée si vide)", Left = 20, Top = y, Width = 380, ForeColor = Color.DimGray };
            y += 23;
            txtReference = new Guna2TextBox { Left = 20, Top = y, Width = 380, Height = 36, BorderRadius = 6, PlaceholderText = "Laisser vide pour génération automatique" };
            y += 50;

            var lblCat = new Label { Text = "Catégorie (optionnel)", Left = 20, Top = y, Width = 380, ForeColor = Color.DimGray };
            y += 23;
            cmbCategorie = new Guna2ComboBox { Left = 20, Top = y, Width = 275, Height = 36, BorderRadius = 6, DropDownStyle = ComboBoxStyle.DropDownList };
            btnNouvelleCategorie = new Guna2Button { Left = 305, Top = y, Width = 95, Height = 36, Text = "+ Nouvelle", BorderRadius = 6, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold) };
            btnNouvelleCategorie.Click += BtnNouvelleCategorie_Click;
            y += 50;

            var lblMarq = new Label { Text = "Marque (optionnel)", Left = 20, Top = y, Width = 380, ForeColor = Color.DimGray };
            y += 23;
            cmbMarque = new Guna2ComboBox { Left = 20, Top = y, Width = 275, Height = 36, BorderRadius = 6, DropDownStyle = ComboBoxStyle.DropDownList };
            btnNouvelleMarque = new Guna2Button { Left = 305, Top = y, Width = 95, Height = 36, Text = "+ Nouvelle", BorderRadius = 6, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold) };
            btnNouvelleMarque.Click += BtnNouvelleMarque_Click;
            y += 55;

            btnEnregistrer = new Guna2Button { Text = "Enregistrer", Left = 200, Top = y, Width = 100, Height = 36, BorderRadius = 6, FillColor = Color.FromArgb(59, 130, 246), ForeColor = Color.White };
            btnAnnuler = new Guna2Button { Text = "Annuler", Left = 310, Top = y, Width = 90, Height = 36, BorderRadius = 6, FillColor = Color.Gray, ForeColor = Color.White };
            btnEnregistrer.Click += BtnEnregistrer_Click;
            btnAnnuler.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            Controls.Add(pnlHeader);
            Controls.Add(lblDesig); Controls.Add(txtDesignation);
            Controls.Add(lblRef); Controls.Add(txtReference);
            Controls.Add(lblCat); Controls.Add(cmbCategorie); Controls.Add(btnNouvelleCategorie);
            Controls.Add(lblMarq); Controls.Add(cmbMarque); Controls.Add(btnNouvelleMarque);
            Controls.Add(btnEnregistrer); Controls.Add(btnAnnuler);

            Height = y + 100;
        }

        private void ChargerCategories()
        {
            var t = DatabaseHelper.ExecuteQuery("SELECT id, designation FROM Categorie ORDER BY designation");
            cmbCategorie.DataSource = t;
            cmbCategorie.DisplayMember = "designation";
            cmbCategorie.ValueMember = "id";
            cmbCategorie.SelectedIndex = -1;
        }

        private void ChargerMarques()
        {
            var t = DatabaseHelper.ExecuteQuery("SELECT id, designation FROM Marque ORDER BY designation");
            cmbMarque.DataSource = t;
            cmbMarque.DisplayMember = "designation";
            cmbMarque.ValueMember = "id";
            cmbMarque.SelectedIndex = -1;
        }

        private void ChargerDonnees()
        {
            var t = DatabaseHelper.ExecuteQuery(
                "SELECT reference, designation, categorie_id, marque_id FROM Modele WHERE id=@id",
                new SqliteParameter("@id", _modeleIdEnEdition!.Value));
            if (t.Rows.Count == 0) { Close(); return; }

            var row = t.Rows[0];
            txtDesignation.Text = row["designation"]?.ToString() ?? "";
            txtReference.Text = row["reference"] == DBNull.Value ? "" : row["reference"].ToString();
            if (row["categorie_id"] != DBNull.Value) cmbCategorie.SelectedValue = Convert.ToInt32(row["categorie_id"]);
            if (row["marque_id"] != DBNull.Value) cmbMarque.SelectedValue = Convert.ToInt32(row["marque_id"]);
        }

        private void BtnNouvelleCategorie_Click(object? sender, EventArgs e)
        {
            using (var frm = new FrmAjouterCategorie())
            {
                if (frm.ShowDialog(this) == DialogResult.OK && frm.CategorieIdResultat.HasValue)
                {
                    ChargerCategories();
                    cmbCategorie.SelectedValue = frm.CategorieIdResultat.Value;
                }
            }
        }

        private void BtnNouvelleMarque_Click(object? sender, EventArgs e)
        {
            using (var frm = new FrmAjouterMarque())
            {
                if (frm.ShowDialog(this) == DialogResult.OK && frm.MarqueIdResultat.HasValue)
                {
                    ChargerMarques();
                    cmbMarque.SelectedValue = frm.MarqueIdResultat.Value;
                }
            }
        }

        private void BtnEnregistrer_Click(object? sender, EventArgs e)
        {
            // Seule la désignation est réellement obligatoire.
            if (string.IsNullOrWhiteSpace(txtDesignation.Text))
            {
                MessageBox.Show("La désignation est obligatoire : c'est le seul moyen d'identifier ce modèle.",
                    "Champ manquant", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                using (var cmd = conn.CreateCommand())
                {
                    object catValue = cmbCategorie.SelectedValue ?? (object)DBNull.Value;
                    object marqValue = cmbMarque.SelectedValue ?? (object)DBNull.Value;
                    // Référence vide -> NULL -> le trigger SQL générera automatiquement une valeur.
                    object refValue = string.IsNullOrWhiteSpace(txtReference.Text) ? (object)DBNull.Value : txtReference.Text.Trim();

                    if (EnModeEdition)
                    {
                        cmd.CommandText = @"
                            UPDATE Modele
                            SET designation=@desig, reference=@ref, categorie_id=@cat, marque_id=@marq, date_modification=CURRENT_TIMESTAMP
                            WHERE id=@id";
                        cmd.Parameters.AddWithValue("@id", _modeleIdEnEdition!.Value);
                        ModeleIdResultat = _modeleIdEnEdition;
                    }
                    else
                    {
                        cmd.CommandText = @"
                            INSERT INTO Modele (designation, reference, categorie_id, marque_id)
                            VALUES (@desig, @ref, @cat, @marq);
                            SELECT last_insert_rowid();";
                    }

                    cmd.Parameters.AddWithValue("@desig", txtDesignation.Text.Trim());
                    cmd.Parameters.AddWithValue("@ref", refValue);
                    cmd.Parameters.AddWithValue("@cat", catValue);
                    cmd.Parameters.AddWithValue("@marq", marqValue);

                    if (!EnModeEdition)
                        ModeleIdResultat = Convert.ToInt32(cmd.ExecuteScalar());
                    else
                        cmd.ExecuteNonQuery();
                }
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
            {
                MessageBox.Show("Cette référence existe déjà pour un autre modèle.", "Doublon",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}