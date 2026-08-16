#nullable enable
using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using Guna.UI2.WinForms;
using InventoryApp.Data;

namespace InventoryApp
{
    public class LigneMouvementTemp
    {
        public int EquipementId { get; set; }
        public string Affichage { get; set; } = "";
        public string Etat { get; set; } = "Bon";
        public bool EstSortie { get; set; } = true;
        public string? Observation { get; set; }
    }

    public class FrmAjouterLigneMouvement : Form
    {
        private readonly Color _primaryBlue = Color.FromArgb(37, 99, 235);
        private readonly Color _darkNavy = Color.FromArgb(24, 30, 54);
        private readonly Color _lightGray = Color.FromArgb(240, 242, 245);

        public LigneMouvementTemp? LigneResultat { get; private set; } = null;

        private readonly LigneMouvementTemp? _ligneAModifier;
        private bool EnModeEdition => _ligneAModifier != null;

        private Guna2ComboBox cmbModele = null!;
        private Guna2ComboBox cmbEquipement = null!;
        private Guna2ComboBox cmbEtat = null!;
        private Guna2ToggleSwitch tglEstSortie = null!;
        private Label lblToggleEtat = null!;
        private Guna2TextBox txtObservation = null!;
        private Guna2Button btnAjouter = null!;
        private Guna2Button btnAnnuler = null!;
        private Label lblStatutActuel = null!;

        public FrmAjouterLigneMouvement(LigneMouvementTemp? ligneAModifier = null)
        {
            _ligneAModifier = ligneAModifier;

            Text = EnModeEdition ? "Modifier la ligne de mouvement" : "Ajouter une ligne de mouvement";
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
                Text = EnModeEdition ? "MODIFIER LIGNE DE MOUVEMENT" : "AJOUTER LIGNE DE MOUVEMENT",
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

            MakeLabel("Modèle *");
            cmbModele = new Guna2ComboBox { Left = marge, Top = y, Width = largeur, Height = 36, BorderRadius = 6, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbModele.SelectedIndexChanged += (s, e) => ChargerEquipementsDuModele(null);
            Controls.Add(cmbModele); y += 42;

            MakeLabel("Équipement (N° série / statut actuel) *");
            cmbEquipement = new Guna2ComboBox { Left = marge, Top = y, Width = largeur, Height = 36, BorderRadius = 6, DropDownStyle = ComboBoxStyle.DropDownList, Enabled = false };
            cmbEquipement.SelectedIndexChanged += (s, e) => AfficherStatutActuel();
            Controls.Add(cmbEquipement); y += 40;

            lblStatutActuel = new Label { Left = marge, Top = y, Width = largeur, Font = new Font("Segoe UI", 8.5F, FontStyle.Italic), ForeColor = _primaryBlue, Text = "" };
            Controls.Add(lblStatutActuel); y += 22;

            MakeLabel("État à ce moment");
            cmbEtat = new Guna2ComboBox { Left = marge, Top = y, Width = largeur, Height = 36, BorderRadius = 6, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbEtat.Items.AddRange(new object[] { "Neuf", "Bon", "Usé", "Endommagé", "Hors service" });
            cmbEtat.SelectedIndex = 1;
            Controls.Add(cmbEtat); y += 45;

            lblToggleEtat = new Label { Left = marge, Top = y + 4, Width = 280, Font = new Font("Segoe UI", 8.5F, FontStyle.Regular), ForeColor = _darkNavy, Text = "Sortie (l'équipement quitte le stock)" };
            tglEstSortie = new Guna2ToggleSwitch { Left = marge + 290, Top = y, Checked = true, CheckedState = { FillColor = _primaryBlue } };
            tglEstSortie.CheckedChanged += (s, e) =>
                lblToggleEtat.Text = tglEstSortie.Checked
                    ? "Sortie (l'équipement quitte le stock)"
                    : "Retour (l'équipement revient au stock)";
            Controls.Add(lblToggleEtat);
            Controls.Add(tglEstSortie); y += 38;

            MakeLabel("Observation (optionnel)");
            txtObservation = new Guna2TextBox { Left = marge, Top = y, Width = largeur, Height = 36, BorderRadius = 6 };
            Controls.Add(txtObservation); y += 50;

            btnAjouter = new Guna2Button
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

            btnAjouter.Click += BtnAjouter_Click;
            btnAnnuler.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            Controls.Add(btnAjouter);
            Controls.Add(btnAnnuler);
        }

        private void ChargerModeles()
        {
            var t = DatabaseHelper.ExecuteQuery("SELECT id, designation FROM Modele ORDER BY designation");
            cmbModele.DisplayMember = "designation";
            cmbModele.ValueMember = "id";
            cmbModele.DataSource = t;

            if (EnModeEdition && _ligneAModifier != null)
            {
                var infosEq = DatabaseHelper.ExecuteQuery(
                    "SELECT modele_id FROM Equipement WHERE id = @id",
                    new Microsoft.Data.Sqlite.SqliteParameter("@id", _ligneAModifier.EquipementId));

                if (infosEq.Rows.Count > 0)
                {
                    int modeleId = Convert.ToInt32(infosEq.Rows[0]["modele_id"]);
                    cmbModele.SelectedValue = modeleId;
                    ChargerEquipementsDuModele(_ligneAModifier.EquipementId);
                }

                cmbEtat.SelectedItem = _ligneAModifier.Etat;
                tglEstSortie.Checked = _ligneAModifier.EstSortie;
                txtObservation.Text = _ligneAModifier.Observation ?? "";
            }
        }

        private void ChargerEquipementsDuModele(int? preselectionnerId)
        {
            cmbEquipement.Enabled = false;
            cmbEquipement.DataSource = null;
            lblStatutActuel.Text = "";

            if (cmbModele.SelectedValue == null || !int.TryParse(cmbModele.SelectedValue.ToString(), out int modeleId))
                return;

            string sql = @"
                SELECT id,
                       COALESCE(numero_serie, 'S/N: -') || '  —  ' || statut AS affichage,
                       statut
                FROM Equipement
                WHERE modele_id = @modeleId
                ORDER BY id DESC";

            var t = DatabaseHelper.ExecuteQuery(sql,
                new Microsoft.Data.Sqlite.SqliteParameter("@modeleId", modeleId));

            if (t.Rows.Count == 0)
            {
                lblStatutActuel.Text = "Aucun équipement enregistré pour ce modèle.";
                return;
            }

            cmbEquipement.DisplayMember = "affichage";
            cmbEquipement.ValueMember = "id";
            cmbEquipement.DataSource = t;
            cmbEquipement.Enabled = true;

            if (preselectionnerId.HasValue)
                cmbEquipement.SelectedValue = preselectionnerId.Value;

            AfficherStatutActuel();
        }

        private void AfficherStatutActuel()
        {
            if (cmbEquipement.SelectedItem is DataRowView rowView)
            {
                string statut = rowView["statut"]?.ToString() ?? "";
                lblStatutActuel.Text = $"Statut actuel : {statut}";
                lblStatutActuel.ForeColor = statut == "Réformé" ? Color.Red : _primaryBlue;
            }
            else
            {
                lblStatutActuel.Text = "";
            }
        }

        private void BtnAjouter_Click(object? sender, EventArgs e)
        {
            if (cmbModele.SelectedValue == null || cmbEquipement.SelectedValue == null)
            {
                MessageBox.Show("Veuillez choisir un modèle puis un équipement.", "Champs manquants",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (lblStatutActuel.Text.Contains("Réformé"))
            {
                MessageBox.Show("Cet équipement est réformé et ne peut plus faire l'objet d'un mouvement.",
                    "Mouvement refusé", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            LigneResultat = new LigneMouvementTemp
            {
                EquipementId = Convert.ToInt32(cmbEquipement.SelectedValue),
                Affichage = $"{cmbModele.Text}  |  {cmbEquipement.Text}",
                Etat = cmbEtat.SelectedItem?.ToString() ?? "Bon",
                EstSortie = tglEstSortie.Checked,
                Observation = string.IsNullOrWhiteSpace(txtObservation.Text) ? null : txtObservation.Text.Trim()
            };

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}