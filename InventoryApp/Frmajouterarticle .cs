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
    /// <summary>
    /// Formulaire simplifié d'ajout/modification d'un équipement.
    /// La gestion des Modèles/Marques/Catégories est déportée vers
    /// FrmGererModeles (bouton "Gérer les modèles"), ce qui allège
    /// considérablement ce formulaire.
    /// </summary>
    public class FrmAjouterArticle : Form
    {
        public bool EquipementAjoute { get; private set; } = false;
        private readonly Form1? _mainForm;

        private readonly int? _equipementIdEnEdition;
        private bool EnModeEdition => _equipementIdEnEdition.HasValue;

        private string? _codeBarreActuel = null;

        private Guna2BorderlessForm borderlessForm = null!;
        private Guna2Panel pnlHeader = null!;
        private Label lblHeaderTitle = null!;
        private Guna2ControlBox btnCloseHeader = null!;

        private Guna2ComboBox cmbModele = null!;
        private Guna2Button btnGererModeles = null!;

        private Guna2TextBox txtNumeroSerie = null!;
        private Guna2DateTimePicker dtpDateAcquisition = null!;
        private Guna2ComboBox cmbStatut = null!;
        private Guna2ComboBox cmbEtat = null!;
        private Guna2TextBox txtEmplacement = null!;
        private Guna2TextBox txtObservations = null!;
        private Guna2CheckBox chkGenererBarcode = null!;
        private Label lblCodeBarreActuel = null!;

        private Guna2Button btnEnregistrer = null!;
        private Guna2Button btnAnnuler = null!;

        private const int MARGE = 20;
        private const int LARGEUR_CHAMP = 400;

        public FrmAjouterArticle() : this(null, null) { }
        public FrmAjouterArticle(Form1? mainForm) : this(mainForm, null) { }

        public FrmAjouterArticle(Form1? mainForm, int? equipementIdEnEdition)
        {
            _mainForm = mainForm;
            _equipementIdEnEdition = equipementIdEnEdition;

            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.FromArgb(248, 250, 252);
            Width = 460;
            Height = 560;

            ConstruireControles();

            Load += (s, e) =>
            {
                ChargerModeles();
                if (EnModeEdition) ChargerDonneesEquipement();
            };
        }

        private void ConstruireControles()
        {
            borderlessForm = new Guna2BorderlessForm { ContainerControl = this, BorderRadius = 14, DragForm = true, HasFormShadow = true };

            pnlHeader = new Guna2Panel { Dock = DockStyle.Top, Height = 50, FillColor = Color.FromArgb(30, 41, 59) };
            lblHeaderTitle = new Label
            {
                Text = EnModeEdition ? $"Modifier l'article #{_equipementIdEnEdition}" : "Ajouter un article",
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

            var lblModele = new Label { Text = "Modèle *", Left = MARGE, Top = y, Width = 300, ForeColor = Color.DimGray };
            y += 23;
            cmbModele = new Guna2ComboBox { Left = MARGE, Top = y, Width = 275, Height = 36, BorderRadius = 6, Font = new Font("Segoe UI", 9.5F), DropDownStyle = ComboBoxStyle.DropDownList };
            btnGererModeles = new Guna2Button { Left = MARGE + 285, Top = y, Width = 115, Height = 36, Text = "+ Ajouter", Font = new Font("Segoe UI", 9F, FontStyle.Bold), BorderRadius = 6, FillColor = Color.FromArgb(100, 116, 139), ForeColor = Color.White, Cursor = Cursors.Hand };
            btnGererModeles.Click += BtnGererModeles_Click;
            y += 48;

            var lblNumSerie = new Label { Text = "N° série (optionnel)", Left = MARGE, Top = y, Width = LARGEUR_CHAMP, ForeColor = Color.DimGray };
            y += 23;
            txtNumeroSerie = new Guna2TextBox { Left = MARGE, Top = y, Width = LARGEUR_CHAMP, Height = 36, BorderRadius = 6 };
            y += 48;

            var lblEmplacement = new Label { Text = "Emplacement", Left = MARGE, Top = y, Width = LARGEUR_CHAMP, ForeColor = Color.DimGray };
            y += 23;
            txtEmplacement = new Guna2TextBox { Left = MARGE, Top = y, Width = LARGEUR_CHAMP, Height = 36, BorderRadius = 6, PlaceholderText = "Non précisé" };
            y += 48;

            var lblDate = new Label { Text = "Date d'acquisition", Left = MARGE, Top = y, Width = 190, ForeColor = Color.DimGray };
            var lblStatut = new Label { Text = "Statut", Left = MARGE + 200, Top = y, Width = 190, ForeColor = Color.DimGray };
            y += 23;
            dtpDateAcquisition = new Guna2DateTimePicker { Left = MARGE, Top = y, Width = 190, Height = 36, Format = DateTimePickerFormat.Short, Value = DateTime.Today, BorderRadius = 6 };
            cmbStatut = new Guna2ComboBox { Left = MARGE + 200, Top = y, Width = 200, Height = 36, BorderRadius = 6, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbStatut.Items.AddRange(new object[] { "En stock", "Affecté", "En prêt", "En panne", "En réparation", "Réformé" });
            cmbStatut.SelectedIndex = 0;
            y += 48;

            var lblEtat = new Label { Text = "État", Left = MARGE, Top = y, Width = LARGEUR_CHAMP, ForeColor = Color.DimGray };
            y += 23;
            cmbEtat = new Guna2ComboBox { Left = MARGE, Top = y, Width = 190, Height = 36, BorderRadius = 6, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbEtat.Items.AddRange(new object[] { "Neuf", "Bon", "Usé", "Endommagé", "Hors service" });
            cmbEtat.SelectedIndex = 1;
            y += 48;

            var lblObservations = new Label { Text = "Observations", Left = MARGE, Top = y, Width = LARGEUR_CHAMP, ForeColor = Color.DimGray };
            y += 23;
            txtObservations = new Guna2TextBox { Left = MARGE, Top = y, Width = LARGEUR_CHAMP, Height = 36, BorderRadius = 6 };
            y += 48;

            chkGenererBarcode = new Guna2CheckBox { Text = "Générer le code-barre immédiatement", Left = MARGE, Top = y, Width = LARGEUR_CHAMP };
            lblCodeBarreActuel = new Label { Left = MARGE, Top = y, Width = LARGEUR_CHAMP, ForeColor = Color.FromArgb(21, 128, 61), Visible = false };
            y += 45;

            btnEnregistrer = new Guna2Button { Text = EnModeEdition ? "Enregistrer les modifications" : "Enregistrer", Left = MARGE, Top = y, Width = EnModeEdition ? 260 : 180, Height = 40, BorderRadius = 8, FillColor = Color.FromArgb(59, 130, 246), ForeColor = Color.White, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold) };
            btnAnnuler = new Guna2Button { Text = "Annuler", Left = Width - 120, Top = y, Width = 90, Height = 40, BorderRadius = 8, FillColor = Color.Gray, ForeColor = Color.White };
            btnEnregistrer.Click += BtnEnregistrer_Click;
            btnAnnuler.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            y += 60;

            Controls.Add(pnlHeader);
            Controls.Add(lblModele); Controls.Add(cmbModele); Controls.Add(btnGererModeles);
            Controls.Add(lblNumSerie); Controls.Add(txtNumeroSerie);
            Controls.Add(lblEmplacement); Controls.Add(txtEmplacement);
            Controls.Add(lblDate); Controls.Add(dtpDateAcquisition);
            Controls.Add(lblStatut); Controls.Add(cmbStatut);
            Controls.Add(lblEtat); Controls.Add(cmbEtat);
            Controls.Add(lblObservations); Controls.Add(txtObservations);
            Controls.Add(chkGenererBarcode); Controls.Add(lblCodeBarreActuel);
            Controls.Add(btnEnregistrer); Controls.Add(btnAnnuler);

            Height = y + 40;
        }

        private void ChargerModeles()
        {
            string sql = @"
                SELECT md.id,
                       md.designation || ' (' ||
                           COALESCE(c.designation, 'sans catégorie') || ' / ' ||
                           COALESCE(m.designation, 'sans marque') || ')' AS affichage
                FROM Modele md
                LEFT JOIN Categorie c ON md.categorie_id = c.id
                LEFT JOIN Marque m ON md.marque_id = m.id
                ORDER BY md.designation";
            var t = DatabaseHelper.ExecuteQuery(sql);
            cmbModele.DataSource = t;
            cmbModele.DisplayMember = "affichage";
            cmbModele.ValueMember = "id";
        }

        private void BtnGererModeles_Click(object? sender, EventArgs e)
        {
            using (var frm = new FrmAjouterModele())
            {
                if (frm.ShowDialog(this) == DialogResult.OK)
                {
                    // 1. Recharger la ComboBox avec les nouveaux modèles de la base de données
                    ChargerModeles();

                    // 2. Sélectionner automatiquement le modèle nouvellement créé
                    if (frm.ModeleIdResultat.HasValue)
                    {
                        cmbModele.SelectedValue = frm.ModeleIdResultat.Value;
                    }
                }
            }
        }

        private void ChargerDonneesEquipement()
        {
            var t = DatabaseHelper.ExecuteQuery(
                "SELECT modele_id, numero_serie, statut, etat, date_acquisition, code_barre, emplacement, observations FROM Equipement WHERE id = @id",
                new SqliteParameter("@id", _equipementIdEnEdition!.Value));

            if (t.Rows.Count == 0)
            {
                MessageBox.Show("Équipement introuvable (peut-être déjà supprimé).", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close();
                return;
            }

            var row = t.Rows[0];
            cmbModele.SelectedValue = Convert.ToInt32(row["modele_id"]);
            txtNumeroSerie.Text = row["numero_serie"] == DBNull.Value ? "" : row["numero_serie"].ToString();
            cmbStatut.SelectedItem = row["statut"].ToString();
            cmbEtat.SelectedItem = row["etat"] == DBNull.Value ? "Bon" : row["etat"].ToString();
            txtEmplacement.Text = row["emplacement"]?.ToString() ?? "";
            txtObservations.Text = row["observations"] == DBNull.Value ? "" : row["observations"].ToString();

            if (DateTime.TryParse(row["date_acquisition"].ToString(), out var d))
                dtpDateAcquisition.Value = d;

            _codeBarreActuel = row["code_barre"] == DBNull.Value ? null : row["code_barre"].ToString();
            if (!string.IsNullOrEmpty(_codeBarreActuel))
            {
                chkGenererBarcode.Visible = false;
                lblCodeBarreActuel.Visible = true;
                lblCodeBarreActuel.Text = "Code-barre actuel : " + _codeBarreActuel;
            }
        }

        private void BtnEnregistrer_Click(object? sender, EventArgs e)
        {
            if (cmbModele.SelectedValue == null)
            {
                MessageBox.Show("Veuillez choisir un modèle (bouton \"Gérer...\" pour en créer un).", "Champ manquant", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            try
            {
                if (EnModeEdition) EnregistrerModification();
                else EnregistrerNouvelArticle();

                EquipementAjoute = true;
                _mainForm?.ChargerEquipements();
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
            {
                MessageBox.Show(
                    "Enregistrement refusé.\n\nCauses possibles :\n" +
                    "- Le modèle choisi n'a pas de marque/catégorie\n" +
                    "- Ce numéro de série existe déjà\n\nDétail : " + ex.Message,
                    "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur : " + ex.Message, "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string EmplacementValeur()
            => string.IsNullOrWhiteSpace(txtEmplacement.Text) ? "Non précisé" : txtEmplacement.Text.Trim();

        private void EnregistrerNouvelArticle()
        {
            string sql = @"
                INSERT INTO Equipement (modele_id, numero_serie, statut, etat, emplacement, observations, date_acquisition, barcode_genere)
                VALUES (@modeleId, @numSerie, @statut, @etat, @emplacement, @obs, @dateAcq, @barGen);";

            using (var conn = DatabaseHelper.GetConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = sql;
                cmd.Parameters.AddWithValue("@modeleId", cmbModele.SelectedValue);
                cmd.Parameters.AddWithValue("@numSerie", string.IsNullOrWhiteSpace(txtNumeroSerie.Text) ? (object)DBNull.Value : txtNumeroSerie.Text.Trim());
                cmd.Parameters.AddWithValue("@statut", cmbStatut.SelectedItem?.ToString() ?? "En stock");
                cmd.Parameters.AddWithValue("@etat", cmbEtat.SelectedItem?.ToString() ?? "Bon");
                cmd.Parameters.AddWithValue("@emplacement", EmplacementValeur());
                cmd.Parameters.AddWithValue("@obs", string.IsNullOrWhiteSpace(txtObservations.Text) ? (object)DBNull.Value : txtObservations.Text.Trim());
                cmd.Parameters.AddWithValue("@dateAcq", dtpDateAcquisition.Value.ToString("yyyy-MM-dd"));
                cmd.Parameters.AddWithValue("@barGen", chkGenererBarcode.Checked ? 1 : 0);
                cmd.ExecuteNonQuery();
            }
        }

        private void EnregistrerModification()
        {
            bool demandeGeneration = chkGenererBarcode.Visible && chkGenererBarcode.Checked;

            string sql = @"
                UPDATE Equipement
                SET modele_id = @modeleId,
                    numero_serie = @numSerie,
                    statut = @statut,
                    etat = @etat,
                    emplacement = @emplacement,
                    observations = @obs,
                    date_acquisition = @dateAcq,
                    date_modification = CURRENT_TIMESTAMP" +
                    (demandeGeneration ? ", barcode_genere = 1" : "") + @"
                WHERE id = @id;";

            using (var conn = DatabaseHelper.GetConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = sql;
                cmd.Parameters.AddWithValue("@modeleId", cmbModele.SelectedValue);
                cmd.Parameters.AddWithValue("@numSerie", string.IsNullOrWhiteSpace(txtNumeroSerie.Text) ? (object)DBNull.Value : txtNumeroSerie.Text.Trim());
                cmd.Parameters.AddWithValue("@statut", cmbStatut.SelectedItem?.ToString() ?? "En stock");
                cmd.Parameters.AddWithValue("@etat", cmbEtat.SelectedItem?.ToString() ?? "Bon");
                cmd.Parameters.AddWithValue("@emplacement", EmplacementValeur());
                cmd.Parameters.AddWithValue("@obs", string.IsNullOrWhiteSpace(txtObservations.Text) ? (object)DBNull.Value : txtObservations.Text.Trim());
                cmd.Parameters.AddWithValue("@dateAcq", dtpDateAcquisition.Value.ToString("yyyy-MM-dd"));
                cmd.Parameters.AddWithValue("@id", _equipementIdEnEdition!.Value);
                cmd.ExecuteNonQuery();
            }
        }
    }
}