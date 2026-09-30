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
    // Ligne temporaire : référence directement un MODÈLE (plus d'équipement)
    public class LigneInventaireTemp
    {
        public int ModeleId { get; set; }
        public string AffichageModele { get; set; } = "";
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

        // Sert à ne présélectionner la ligne à modifier qu'au premier chargement
        private bool _premierChargement = true;

        private Guna2ComboBox cmbModele = null!;
        private Guna2Button btnNouveauModele = null!;
        private Guna2NumericUpDown numQuantite = null!;
        private Guna2TextBox txtObservation = null!;
        private Guna2Button btnValider = null!;
        private Guna2Button btnAnnuler = null!;
        private readonly ToolTip _toolTip = new ToolTip();

        public FrmAjouterLigneInventaire(LigneInventaireTemp? ligneAModifier = null)
        {
            _ligneAModifier = ligneAModifier;

            Text = EnModeEdition ? "Modifier la ligne d'inventaire" : "Ajouter une ligne d'inventaire";
            Size = new Size(460, 380);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.None;
            BackColor = Color.White;

            ConstruireControles();
            Load += (s, e) => ChargerModeles();
        }

        private void ConstruireControles()
        {
            var panelHeader = new Panel { Dock = DockStyle.Top, Height = 55, BackColor = _darkNavy };
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

            // --- Modèle + bouton "+" ---
            MakeLabel("Modèle * (Catégorie · Marque · Désignation · Référence)");
            cmbModele = new Guna2ComboBox
            {
                Left = marge,
                Top = y,
                Width = largeur - 46,   // on laisse la place au bouton
                Height = 36,
                BorderRadius = 6,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            Controls.Add(cmbModele);

            btnNouveauModele = new Guna2Button
            {
                Text = "+",
                Left = marge + largeur - 40,
                Top = y,
                Width = 40,
                Height = 36,
                BorderRadius = 6,
                FillColor = _primaryBlue,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnNouveauModele.Click += BtnNouveauModele_Click;
            Controls.Add(btnNouveauModele);
            _toolTip.SetToolTip(btnNouveauModele, "Créer un nouveau modèle (article)");

            y += 42;

            // --- Quantité ---
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

            // --- Observation ---
            MakeLabel("Observation (optionnel)");
            txtObservation = new Guna2TextBox { Left = marge, Top = y, Width = largeur, Height = 36, BorderRadius = 6 };
            Controls.Add(txtObservation); y += 55;

            // --- Boutons ---
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
                       TRIM(COALESCE(c.designation,'') || ' ' || COALESCE(mq.designation,'') || ' ' ||
                            m.designation || ' ' || COALESCE(m.reference,'')) AS affichage
                FROM Modele m
                LEFT JOIN Categorie c ON m.categorie_id = c.id
                LEFT JOIN Marque mq ON m.marque_id = mq.id
                ORDER BY c.designation, m.designation";

            var t = DatabaseHelper.ExecuteQuery(sql);
            cmbModele.DataSource = t;
            cmbModele.DisplayMember = "affichage";
            cmbModele.ValueMember = "id";

            // Présélection uniquement au premier chargement (ne pas écraser après création d'un modèle)
            if (_premierChargement && EnModeEdition && _ligneAModifier != null)
            {
                // SQLite renvoie des Int64 : la présélection doit être un long
                cmbModele.SelectedValue = (long)_ligneAModifier.ModeleId;
                numQuantite.Value = Math.Max(1, _ligneAModifier.Quantite);
                txtObservation.Text = _ligneAModifier.Observation ?? "";
            }
            _premierChargement = false;
        }

        private void BtnNouveauModele_Click(object? sender, EventArgs e)
        {
            using (var frm = new FrmAjouterModele())
            {
                if (frm.ShowDialog(this) == DialogResult.OK)
                {
                    ChargerModeles();
                    // Sélectionne automatiquement le modèle qui vient d'être créé
                    // (retirez le cast (long) si ModeleIdResultat est déjà un long)
                    cmbModele.SelectedValue = (long)frm.ModeleIdResultat;
                }
            }
        }

        private void BtnValider_Click(object? sender, EventArgs e)
        {
            if (cmbModele.SelectedValue == null)
            {
                MessageBox.Show("Le choix d'un modèle est obligatoire pour ajouter une ligne.", "Champ manquant",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int modeleId = cmbModele.SelectedValue is DataRowView drv
                ? Convert.ToInt32(drv["id"])
                : Convert.ToInt32(cmbModele.SelectedValue);

            LigneResultat = new LigneInventaireTemp
            {
                ModeleId = modeleId,
                AffichageModele = cmbModele.Text,
                Quantite = (int)numQuantite.Value,
                Observation = string.IsNullOrWhiteSpace(txtObservation.Text) ? null : txtObservation.Text.Trim()
            };

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}