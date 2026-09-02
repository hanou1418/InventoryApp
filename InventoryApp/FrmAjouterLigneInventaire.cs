#nullable enable
using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using Guna.UI2.WinForms;
using InventoryApp.Data;
using Microsoft.Data.Sqlite;

namespace InventoryApp
{
    public class LigneInventaireTemp
    {
        public int EquipementId { get; set; }
        public string AffichageModele { get; set; } = "";
        public string AffichageEquipement { get; set; } = "";
        public int Quantite { get; set; } = 1;
        public string? Observation { get; set; }
    }

    public class FrmAjouterLigneInventaire : Form
    {
        private readonly Color _primaryBlue = Color.FromArgb(37, 99, 235);
        private readonly Color _darkNavy = Color.FromArgb(24, 30, 54);
        private readonly Color _lightGray = Color.FromArgb(240, 242, 245);

        public LigneInventaireTemp? LigneResultat { get; private set; } = null;

        private readonly LigneInventaireTemp? _ligneAModifier;
        private bool EnModeEdition => _ligneAModifier != null;

        private Guna2ComboBox cmbModele = null!;
        private Guna2ComboBox cmbEquipement = null!;
        private Guna2NumericUpDown numQuantite = null!;
        private Guna2TextBox txtObservation = null!;
        private Guna2Button btnValider = null!;
        private Guna2Button btnAnnuler = null!;

        public FrmAjouterLigneInventaire(LigneInventaireTemp? ligneAModifier = null)
        {
            _ligneAModifier = ligneAModifier;

            Text = EnModeEdition ? "Modifier la ligne d'inventaire" : "Ajouter une ligne d'inventaire";
            Size = new Size(460, 480);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.None;
            BackColor = Color.White;

            ConstruireControles();
            Load += (s, e) => ChargerModeles();
        }

        private void ConstruireControles()
        {
            // Panel En-tête
            var panelHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 55,
                BackColor = _darkNavy
            };

            var lblTitre = new Label
            {
                Text = EnModeEdition ? "MODIFIER LIGNE D'INVENTAIRE" : "AJOUTER LIGNE D'INVENTAIRE",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            panelHeader.Controls.Add(lblTitre);
            Controls.Add(panelHeader);

            int y = 70;
            const int marge = 25;
            const int largeur = 390;

            Label MakeLabel(string texte)
            {
                var l = new Label
                {
                    Text = texte,
                    Left = marge,
                    Top = y,
                    Width = largeur,
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                    ForeColor = _darkNavy
                };
                Controls.Add(l);
                y += 22;
                return l;
            }

            MakeLabel("Modèle * (Catégorie · Marque · Désignation · Référence)");
            cmbModele = new Guna2ComboBox { Left = marge, Top = y, Width = largeur, Height = 36, BorderRadius = 6, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbModele.SelectedIndexChanged += (s, e) => ChargerEquipementsDuModele(null);
            Controls.Add(cmbModele); y += 42;

            MakeLabel("Équipement * (ID · Statut · État · Code-barre · N° Série)");
            cmbEquipement = new Guna2ComboBox { Left = marge, Top = y, Width = largeur, Height = 36, BorderRadius = 6, DropDownStyle = ComboBoxStyle.DropDownList, Enabled = false };
            Controls.Add(cmbEquipement); y += 42;

            MakeLabel("Quantité *");
            numQuantite = new Guna2NumericUpDown
            {
                Left = marge,
                Top = y,
                Width = 140,
                Height = 36,
                BorderRadius = 6,
                Minimum = 1,
                Maximum = 999999,
                Value = 1,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular)
            };
            Controls.Add(numQuantite); y += 42;

            MakeLabel("Observation (optionnel)");
            txtObservation = new Guna2TextBox { Left = marge, Top = y, Width = largeur, Height = 36, BorderRadius = 6 };
            Controls.Add(txtObservation); y += 55;

            btnValider = new Guna2Button
            {
                Text = EnModeEdition ? "Enregistrer" : "Ajouter à la liste",
                Left = marge + 190,
                Top = y,
                Width = 200,
                Height = 40,
                BorderRadius = 6,
                FillColor = _primaryBlue,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };

            btnAnnuler = new Guna2Button
            {
                Text = "Annuler",
                Left = marge,
                Top = y,
                Width = 175,
                Height = 40,
                BorderRadius = 6,
                FillColor = _lightGray,
                ForeColor = _darkNavy,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };

            btnValider.Click += BtnValider_Click;
            btnAnnuler.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            Controls.Add(btnValider);
            Controls.Add(btnAnnuler);
        }

        private void ChargerModeles()
        {
            string sql = @"
                SELECT m.id,
                       TRIM(COALESCE(c.designation,'') || ' ' || COALESCE(mq.designation,'') || ' ' || m.designation || ' ' || COALESCE(m.reference,'')) AS affichage
                FROM Modele m
                LEFT JOIN Categorie c ON m.categorie_id = c.id
                LEFT JOIN Marque mq ON m.marque_id = mq.id
                ORDER BY m.designation";

            var t = DatabaseHelper.ExecuteQuery(sql);
            cmbModele.DataSource = t;
            cmbModele.DisplayMember = "affichage";
            cmbModele.ValueMember = "id";

            if (EnModeEdition && _ligneAModifier != null)
            {
                var infosEq = DatabaseHelper.ExecuteQuery(
                    "SELECT modele_id FROM Equipement WHERE id = @id",
                    new SqliteParameter("@id", _ligneAModifier.EquipementId));

                if (infosEq.Rows.Count > 0)
                {
                    int modeleId = Convert.ToInt32(infosEq.Rows[0]["modele_id"]);
                    cmbModele.SelectedValue = modeleId;
                    ChargerEquipementsDuModele(_ligneAModifier.EquipementId);
                }

                numQuantite.Value = _ligneAModifier.Quantite;
                txtObservation.Text = _ligneAModifier.Observation ?? "";
            }
        }

        private void ChargerEquipementsDuModele(int? preselectionnerId)
        {
            cmbEquipement.Enabled = false;
            cmbEquipement.DataSource = null;

            if (cmbModele.SelectedValue == null) return;

            // Déballage sécurisé pour éviter le DataRowView
            int modeleId;
            if (cmbModele.SelectedValue is DataRowView drv)
            {
                modeleId = Convert.ToInt32(drv["id"]);
            }
            else
            {
                modeleId = Convert.ToInt32(cmbModele.SelectedValue);
            }

            string sql = @"
                SELECT id,
                       id || '  |  ' || statut || '  |  ' || COALESCE(etat,'-') || '  |  ' || COALESCE(code_barre,'-') || '  |  ' || COALESCE(numero_serie,'-') AS affichage
                FROM Equipement
                WHERE modele_id = @modeleId
                ORDER BY id DESC";

            var t = DatabaseHelper.ExecuteQuery(sql, new SqliteParameter("@modeleId", modeleId));

            if (t.Rows.Count == 0) return;

            cmbEquipement.DataSource = t;
            cmbEquipement.DisplayMember = "affichage";
            cmbEquipement.ValueMember = "id";
            cmbEquipement.Enabled = true;

            if (preselectionnerId.HasValue)
                cmbEquipement.SelectedValue = preselectionnerId.Value;
        }

        private void BtnValider_Click(object? sender, EventArgs e)
        {
            if (cmbModele.SelectedValue == null || cmbEquipement.SelectedValue == null)
            {
                MessageBox.Show("Le choix d'un équipement est obligatoire pour ajouter une ligne.", "Champ manquant",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (numQuantite.Value < 1)
            {
                MessageBox.Show("La quantité doit être au moins égale à 1.", "Quantité invalide",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            LigneResultat = new LigneInventaireTemp
            {
                EquipementId = Convert.ToInt32(cmbEquipement.SelectedValue),
                AffichageModele = cmbModele.Text,
                AffichageEquipement = cmbEquipement.Text,
                Quantite = (int)numQuantite.Value,
                Observation = string.IsNullOrWhiteSpace(txtObservation.Text) ? null : txtObservation.Text.Trim()
            };

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}