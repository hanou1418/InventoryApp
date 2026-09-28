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
    /// Popup Guna.UI2 pour ajouter ou modifier un Modele.
    /// Permet la saisie d'une quantité initiale à la création.
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
        private Guna2NumericUpDown numQteAlerte = null!;
        private Guna2NumericUpDown numQteInitiale = null!;

        private Guna2TextBox txtEmplacement = null!;
        private Guna2TextBox txtObservation = null!;

        private Guna2Button btnEnregistrer = null!;
        private Guna2Button btnAnnuler = null!;

        public FrmAjouterModele(int? modeleIdEnEdition = null)
        {
            _modeleIdEnEdition = modeleIdEnEdition;

            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.FromArgb(248, 250, 252);
            Width = 430;

            ConstruireControles();
            Load += (s, e) => { ChargerCategories(); ChargerMarques(); if (EnModeEdition) ChargerDonnees(); };
        }

        private void ConstruireControles()
        {
            borderlessForm = new Guna2BorderlessForm { ContainerControl = this, BorderRadius = 14, DragForm = true, HasFormShadow = true };

            // 1. Header (Largeur élargie à 620px)
            Width = 620;
            pnlHeader = new Guna2Panel { Dock = DockStyle.Top, Height = 48, FillColor = Color.FromArgb(30, 41, 59) };
            lblHeaderTitle = new Label
            {
                Text = EnModeEdition ? "Modifier l'article" : "Nouveau article",
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                AutoSize = true,
                Left = 20,
                Top = 13
            };
            btnCloseHeader = new Guna2ControlBox
            {
                ControlBoxType = Guna.UI2.WinForms.Enums.ControlBoxType.CloseBox,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Left = Width - 40,
                Top = 9,
                Size = new Size(30, 30),
                FillColor = Color.Transparent,
                IconColor = Color.White,
                BorderRadius = 6
            };
            pnlHeader.Controls.Add(lblHeaderTitle);
            pnlHeader.Controls.Add(btnCloseHeader);

            // Positions des colonnes
            int col1_X = 20;
            int col2_X = 315;
            int colWidth = 285;

            // --- Ligne 1 : Désignation (colonne 1) | Référence (colonne 2) ---
            int y1 = 60;
            var lblDesig = new Label { Text = "Désignation *", Left = col1_X, Top = y1, AutoSize = true, ForeColor = Color.DimGray };
            txtDesignation = new Guna2TextBox { Left = col1_X, Top = y1 + 20, Width = colWidth, Height = 36, BorderRadius = 6, PlaceholderText = "Ex : HP LaserJet 1020" };

            var lblRef = new Label { Text = "Référence (optionnel)", Left = col2_X, Top = y1, AutoSize = true, ForeColor = Color.DimGray };
            txtReference = new Guna2TextBox { Left = col2_X, Top = y1 + 20, Width = colWidth, Height = 36, BorderRadius = 6, PlaceholderText = "Auto-générée si vide" };

            // --- Ligne 2 : Catégorie (colonne 1) | Marque (colonne 2) ---
            int y2 = y1 + 65;
            var lblCat = new Label { Text = "Catégorie", Left = col1_X, Top = y2, AutoSize = true, ForeColor = Color.DimGray };
            cmbCategorie = new Guna2ComboBox { Left = col1_X, Top = y2 + 20, Width = 195, Height = 36, BorderRadius = 6, DropDownStyle = ComboBoxStyle.DropDownList };
            btnNouvelleCategorie = new Guna2Button { Left = col1_X + 200, Top = y2 + 20, Width = 85, Height = 36, Text = "+ Créer", BorderRadius = 6, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold) };
            btnNouvelleCategorie.Click += BtnNouvelleCategorie_Click;

            var lblMarq = new Label { Text = "Marque", Left = col2_X, Top = y2, AutoSize = true, ForeColor = Color.DimGray };
            cmbMarque = new Guna2ComboBox { Left = col2_X, Top = y2 + 20, Width = 195, Height = 36, BorderRadius = 6, DropDownStyle = ComboBoxStyle.DropDownList };
            btnNouvelleMarque = new Guna2Button { Left = col2_X + 200, Top = y2 + 20, Width = 85, Height = 36, Text = "+ Créer", BorderRadius = 6, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold) };
            btnNouvelleMarque.Click += BtnNouvelleMarque_Click;

            // --- Ligne 3 : Quantité initiale/actuelle (colonne 1) | Quantité d'alerte (colonne 2) ---
            int y3 = y2 + 65;
            var lblQteInit = new Label { Text = EnModeEdition ? "Stock actuel (lecture seule)" : "Stock initial", Left = col1_X, Top = y3, AutoSize = true, ForeColor = Color.DimGray };
            numQteInitiale = new Guna2NumericUpDown
            {
                Left = col1_X,
                Top = y3 + 20,
                Width = colWidth,
                Height = 36,
                BorderRadius = 6,
                Minimum = 0,
                Maximum = 100000,
                Value = 0,
                //Enabled = !EnModeEdition  ne permer pas de modifier la quantité initiale en mode édition, mais on peut laisser l'utilisateur ajuster le stock actuel.
            };

            var lblQteAlerte = new Label { Text = "Seuil d'alerte", Left = col2_X, Top = y3, AutoSize = true, ForeColor = Color.DimGray };
            numQteAlerte = new Guna2NumericUpDown
            {
                Left = col2_X,
                Top = y3 + 20,
                Width = colWidth,
                Height = 36,
                BorderRadius = 6,
                Minimum = 0,
                Maximum = 10000,
                Value = 0
            };

            // --- Ligne 4 : Emplacement (pleine largeur ou colonne 1) ---
            int y4 = y3 + 65;
            var lblEmpl = new Label { Text = "Emplacement", Left = col1_X, Top = y4, AutoSize = true, ForeColor = Color.DimGray };
            txtEmplacement = new Guna2TextBox { Left = col1_X, Top = y4 + 20, Width = 580, Height = 36, BorderRadius = 6, PlaceholderText = "Ex : Magasin A / Bureau 12" };

            // --- Ligne 5 : Observation (pleine largeur) ---
            int y5 = y4 + 65;
            var lblObs = new Label { Text = "Observation", Left = col1_X, Top = y5, AutoSize = true, ForeColor = Color.DimGray };
            txtObservation = new Guna2TextBox { Left = col1_X, Top = y5 + 20, Width = 580, Height = 50, BorderRadius = 6, Multiline = true, PlaceholderText = "Remarques éventuelles..." };

            // --- Ligne 6 : Boutons d'action ---
            int yBoutons = y5 + 80;
            btnEnregistrer = new Guna2Button { Text = "Enregistrer", Left = 385, Top = yBoutons, Width = 110, Height = 38, BorderRadius = 6, FillColor = Color.FromArgb(59, 130, 246), ForeColor = Color.White };
            btnAnnuler = new Guna2Button { Text = "Annuler", Left = 505, Top = yBoutons, Width = 95, Height = 38, BorderRadius = 6, FillColor = Color.Gray, ForeColor = Color.White };
            btnEnregistrer.Click += BtnEnregistrer_Click;
            btnAnnuler.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            // Ajout des contrôles au formulaire
            Controls.Add(pnlHeader);
            Controls.Add(lblDesig); Controls.Add(txtDesignation);
            Controls.Add(lblRef); Controls.Add(txtReference);
            Controls.Add(lblCat); Controls.Add(cmbCategorie); Controls.Add(btnNouvelleCategorie);
            Controls.Add(lblMarq); Controls.Add(cmbMarque); Controls.Add(btnNouvelleMarque);
            Controls.Add(lblQteInit); Controls.Add(numQteInitiale);
            Controls.Add(lblQteAlerte); Controls.Add(numQteAlerte);
            Controls.Add(lblEmpl); Controls.Add(txtEmplacement);
            Controls.Add(lblObs); Controls.Add(txtObservation);
            Controls.Add(btnEnregistrer); Controls.Add(btnAnnuler);

            Height = yBoutons + 55; // Réduction de la hauteur globale
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
                "SELECT reference, designation, categorie_id, marque_id, quantite, qte_alerte, emplacement, observation FROM Modele WHERE id=@id",
                new SqliteParameter("@id", _modeleIdEnEdition!.Value));
            if (t.Rows.Count == 0) { Close(); return; }

            var row = t.Rows[0];
            txtDesignation.Text = row["designation"]?.ToString() ?? "";
            txtReference.Text = row["reference"] == DBNull.Value ? "" : row["reference"].ToString();
            if (row["categorie_id"] != DBNull.Value) cmbCategorie.SelectedValue = Convert.ToInt64(row["categorie_id"]);
            if (row["marque_id"] != DBNull.Value) cmbMarque.SelectedValue = Convert.ToInt64(row["marque_id"]);
            if (row["quantite"] != DBNull.Value) numQteInitiale.Value = Convert.ToDecimal(row["quantite"]);
            if (row["qte_alerte"] != DBNull.Value) numQteAlerte.Value = Convert.ToDecimal(row["qte_alerte"]);

            txtEmplacement.Text = row["emplacement"] == DBNull.Value ? "" : row["emplacement"].ToString();
            txtObservation.Text = row["observation"] == DBNull.Value ? "" : row["observation"].ToString();
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
            if (string.IsNullOrWhiteSpace(txtDesignation.Text))
            {
                MessageBox.Show("La désignation est obligatoire : c'est le seul moyen d'identifier ce modèle.",
                    "Champ manquant", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                using (var transaction = conn.BeginTransaction())
                {
                    object catValue = cmbCategorie.SelectedValue ?? (object)DBNull.Value;
                    object marqValue = cmbMarque.SelectedValue ?? (object)DBNull.Value;
                    object refValue = string.IsNullOrWhiteSpace(txtReference.Text) ? (object)DBNull.Value : txtReference.Text.Trim();
                    object emplValue = string.IsNullOrWhiteSpace(txtEmplacement.Text) ? (object)DBNull.Value : txtEmplacement.Text.Trim();
                    object obsValue = string.IsNullOrWhiteSpace(txtObservation.Text) ? (object)DBNull.Value : txtObservation.Text.Trim();

                    int newModeleId = 0;

                    if (EnModeEdition)
                    {
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.Transaction = transaction;
                            cmd.CommandText = @"
                                UPDATE Modele
                                SET designation=@desig, categorie_id=@cat, marque_id=@marq, 
                                    reference=COALESCE(@ref, reference, 'AUTO-' || printf('%05d', id)),
                                    qte_alerte=@qteAlerte, emplacement=@empl, observation=@obs, 
                                    date_modification=CURRENT_TIMESTAMP
                                WHERE id=@id";
                            cmd.Parameters.AddWithValue("@id", _modeleIdEnEdition!.Value);
                            cmd.Parameters.AddWithValue("@desig", txtDesignation.Text.Trim());
                            cmd.Parameters.AddWithValue("@ref", refValue);
                            cmd.Parameters.AddWithValue("@cat", catValue);
                            cmd.Parameters.AddWithValue("@marq", marqValue);
                            cmd.Parameters.AddWithValue("@qteAlerte", Convert.ToInt32(numQteAlerte.Value));
                            cmd.Parameters.AddWithValue("@empl", emplValue);
                            cmd.Parameters.AddWithValue("@obs", obsValue);
                            cmd.ExecuteNonQuery();
                        }
                        ModeleIdResultat = _modeleIdEnEdition;
                    }
                    else
                    {
                        // Insertion du modèle avec son stock initial.
                        // (Le trigger trg_auto_reference_modele génère la référence 'AUTO-xxxxx' si @ref est NULL.)
                        // Le stock initial est écrit directement dans Modele.quantite : la table Mouvement
                        // n'accepte que les types Affectation/Prêt/Retour/Maintenance/Réforme.
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.Transaction = transaction;
                            cmd.CommandText = @"
                                INSERT INTO Modele (designation, reference, categorie_id, marque_id, qte_alerte, quantite, emplacement, observation)
                                VALUES (@desig, @ref, @cat, @marq, @qteAlerte, @qte, @empl, @obs);
                                SELECT last_insert_rowid();";
                            cmd.Parameters.AddWithValue("@desig", txtDesignation.Text.Trim());
                            cmd.Parameters.AddWithValue("@ref", refValue);
                            cmd.Parameters.AddWithValue("@cat", catValue);
                            cmd.Parameters.AddWithValue("@marq", marqValue);
                            cmd.Parameters.AddWithValue("@qteAlerte", Convert.ToInt32(numQteAlerte.Value));
                            cmd.Parameters.AddWithValue("@qte", Convert.ToInt32(numQteInitiale.Value));
                            cmd.Parameters.AddWithValue("@empl", emplValue);
                            cmd.Parameters.AddWithValue("@obs", obsValue);

                            newModeleId = Convert.ToInt32(cmd.ExecuteScalar());
                        }

                        ModeleIdResultat = newModeleId;
                    }

                    transaction.Commit();
                }

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
            {
                if (ex.Message.Contains("Modele.reference"))
                    MessageBox.Show("Cette référence existe déjà pour un autre article.", "Doublon",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                else
                    MessageBox.Show("Données refusées par la base : " + ex.Message, "Contrainte non respectée",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur lors de l'enregistrement : " + ex.Message, "Erreur",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}