#nullable enable

using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using InventoryApp.Data;
using Microsoft.Data.Sqlite;

namespace InventoryApp
{
    public class FrmAjouterArticle : Form
    {
        public bool EquipementAjoute { get; private set; } = false;
        private readonly Form1? _mainForm;

        private readonly int? _equipementIdEnEdition;
        private bool EnModeEdition => _equipementIdEnEdition.HasValue;

        private string? _codeBarreActuel = null;

        private ComboBox cmbModele = null!;
        private Button btnToggleNouveauModele = null!;

        private Panel pnlNouveauModele = null!;
        private ComboBox cmbCategorie = null!;
        private Button btnToggleNouvelleCategorie = null!;
        private Panel pnlNouvelleCategorie = null!;
        private TextBox txtCodeCategorie = null!;
        private TextBox txtDesignationCategorie = null!;
        private Button btnEnregistrerCategorie = null!;

        private ComboBox cmbMarque = null!;
        private Button btnToggleNouvelleMarque = null!;
        private Panel pnlNouvelleMarque = null!;
        private TextBox txtCodeMarque = null!;
        private TextBox txtDesignationMarque = null!;
        private Button btnEnregistrerMarque = null!;

        private TextBox txtReferenceModele = null!;
        private TextBox txtDesignationModele = null!;
        private TextBox txtNumeroModeleConstructeur = null!;
        private Button btnEnregistrerModele = null!;

        private TextBox txtNumeroSerie = null!;
        private DateTimePicker dtpDateAcquisition = null!;
        private ComboBox cmbStatut = null!;
        private ComboBox cmbEtat = null!;
        private CheckBox chkGenererBarcode = null!;
        private Label lblCodeBarreActuel = null!;

        private Button btnEnregistrer = null!;
        private Button btnAnnuler = null!;

        private bool showNouveauModele = false;
        private bool showNouvelleCategorie = false;
        private bool showNouvelleMarque = false;

        private const int MARGE = 20;
        private const int LARGEUR_CHAMP = 380;

        // Constructeur de compatibilite (0 argument) : au cas ou du code
        // existant appelle "new FrmAjouterArticle()" sans passer Form1.
        // Equivaut au mode AJOUT sans rafraichissement automatique de Form1.
        public FrmAjouterArticle() : this(null, null) { }

        public FrmAjouterArticle(Form1? mainForm) : this(mainForm, null) { }

        public FrmAjouterArticle(Form1? mainForm, int? equipementIdEnEdition)
        {
            _mainForm = mainForm;
            _equipementIdEnEdition = equipementIdEnEdition;

            Text = EnModeEdition ? $"Modifier l'article #{equipementIdEnEdition}" : "Ajouter un article";
            Width = 460;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            AutoScroll = true;

            ConstruireControles();
            Relayout();

            Load += (s, e) =>
            {
                ChargerCategories();
                ChargerMarques();
                ChargerModeles();
                if (EnModeEdition) ChargerDonneesEquipement();
            };
        }

        private void ConstruireControles()
        {
            cmbModele = new ComboBox { Left = MARGE, Width = 280, DropDownStyle = ComboBoxStyle.DropDownList };
            btnToggleNouveauModele = new Button { Left = MARGE + 290, Width = 110, Text = "+ Nouveau" };
            btnToggleNouveauModele.Click += (s, e) => { showNouveauModele = !showNouveauModele; Relayout(); };

            pnlNouveauModele = new Panel { Left = MARGE, Width = LARGEUR_CHAMP, BorderStyle = BorderStyle.FixedSingle };

            cmbCategorie = new ComboBox { Left = 10, Width = 240, DropDownStyle = ComboBoxStyle.DropDownList };
            btnToggleNouvelleCategorie = new Button { Left = 260, Width = 100, Text = "+ Nouvelle" };
            btnToggleNouvelleCategorie.Click += (s, e) => { showNouvelleCategorie = !showNouvelleCategorie; Relayout(); };

            pnlNouvelleCategorie = new Panel { Left = 10, Width = 350, BorderStyle = BorderStyle.FixedSingle, BackColor = Color.WhiteSmoke };
            txtCodeCategorie = new TextBox { Left = 8, Top = 8, Width = 80, PlaceholderText = "Code (ex: MOB)" };
            txtDesignationCategorie = new TextBox { Left = 96, Top = 8, Width = 160, PlaceholderText = "Désignation" };
            btnEnregistrerCategorie = new Button { Left = 262, Top = 6, Width = 80, Text = "Créer" };
            btnEnregistrerCategorie.Click += BtnEnregistrerCategorie_Click;
            pnlNouvelleCategorie.Controls.AddRange(new Control[] { txtCodeCategorie, txtDesignationCategorie, btnEnregistrerCategorie });
            pnlNouvelleCategorie.Height = 42;

            cmbMarque = new ComboBox { Left = 10, Width = 240, DropDownStyle = ComboBoxStyle.DropDownList };
            btnToggleNouvelleMarque = new Button { Left = 260, Width = 100, Text = "+ Nouvelle" };
            btnToggleNouvelleMarque.Click += (s, e) => { showNouvelleMarque = !showNouvelleMarque; Relayout(); };

            pnlNouvelleMarque = new Panel { Left = 10, Width = 350, BorderStyle = BorderStyle.FixedSingle, BackColor = Color.WhiteSmoke };
            txtCodeMarque = new TextBox { Left = 8, Top = 8, Width = 80, PlaceholderText = "Code (ex: HP)" };
            txtDesignationMarque = new TextBox { Left = 96, Top = 8, Width = 160, PlaceholderText = "Désignation" };
            btnEnregistrerMarque = new Button { Left = 262, Top = 6, Width = 80, Text = "Créer" };
            btnEnregistrerMarque.Click += BtnEnregistrerMarque_Click;
            pnlNouvelleMarque.Controls.AddRange(new Control[] { txtCodeMarque, txtDesignationMarque, btnEnregistrerMarque });
            pnlNouvelleMarque.Height = 42;

            txtReferenceModele = new TextBox { Left = 10, Width = 350, PlaceholderText = "Référence interne (unique)" };
            txtDesignationModele = new TextBox { Left = 10, Width = 350, PlaceholderText = "Désignation (ex: HP LaserJet 1020)" };
            txtNumeroModeleConstructeur = new TextBox { Left = 10, Width = 350, PlaceholderText = "N° modèle constructeur (optionnel)" };
            btnEnregistrerModele = new Button { Left = 250, Width = 110, Text = "Créer le modèle" };
            btnEnregistrerModele.Click += BtnEnregistrerModele_Click;

            pnlNouveauModele.Controls.AddRange(new Control[]
            {
                cmbCategorie, btnToggleNouvelleCategorie, pnlNouvelleCategorie,
                cmbMarque, btnToggleNouvelleMarque, pnlNouvelleMarque,
                txtReferenceModele, txtDesignationModele, txtNumeroModeleConstructeur,
                btnEnregistrerModele
            });

            txtNumeroSerie = new TextBox { Left = MARGE, Width = LARGEUR_CHAMP, PlaceholderText = "N° série (optionnel)" };

            dtpDateAcquisition = new DateTimePicker
            {
                Left = MARGE,
                Width = 200,
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Today
            };

            cmbStatut = new ComboBox { Left = MARGE, Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbStatut.Items.AddRange(new object[] { "En stock", "Affecté", "En prêt", "En panne", "En réparation", "Réformé" });
            cmbStatut.SelectedIndex = 0;

            cmbEtat = new ComboBox { Left = MARGE, Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbEtat.Items.AddRange(new object[] { "Neuf", "Bon", "Usé", "Endommagé", "Hors service" });
            cmbEtat.SelectedIndex = 1; // "Bon" par défaut

            chkGenererBarcode = new CheckBox { Text = "Générer le code-barre immédiatement", Left = MARGE, Width = LARGEUR_CHAMP };
            lblCodeBarreActuel = new Label { Left = MARGE, Width = LARGEUR_CHAMP, ForeColor = Color.DarkGreen, Visible = false };

            btnEnregistrer = new Button { Text = EnModeEdition ? "Enregistrer les modifications" : "Enregistrer", Width = EnModeEdition ? 190 : 100 };
            btnAnnuler = new Button { Text = "Annuler", Width = 90 };
            btnEnregistrer.Click += BtnEnregistrer_Click;
            btnAnnuler.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            Controls.AddRange(new Control[]
            {
                cmbModele, btnToggleNouveauModele, pnlNouveauModele,
                txtNumeroSerie, dtpDateAcquisition, cmbStatut, cmbEtat, chkGenererBarcode, lblCodeBarreActuel,
                btnEnregistrer, btnAnnuler
            });
        }

        private void Relayout()
        {
            int y = 20;

            cmbModele.Top = y;
            btnToggleNouveauModele.Top = y;
            btnToggleNouveauModele.Text = showNouveauModele ? "- Fermer" : "+ Nouveau";
            y += 35;

            pnlNouveauModele.Top = y;
            pnlNouveauModele.Visible = showNouveauModele;

            if (showNouveauModele)
            {
                int yi = 10;
                cmbCategorie.Top = yi;
                btnToggleNouvelleCategorie.Top = yi;
                btnToggleNouvelleCategorie.Text = showNouvelleCategorie ? "- Fermer" : "+ Nouvelle";
                yi += 32;

                pnlNouvelleCategorie.Top = yi;
                pnlNouvelleCategorie.Visible = showNouvelleCategorie;
                if (showNouvelleCategorie) yi += pnlNouvelleCategorie.Height + 8;

                cmbMarque.Top = yi;
                btnToggleNouvelleMarque.Top = yi;
                btnToggleNouvelleMarque.Text = showNouvelleMarque ? "- Fermer" : "+ Nouvelle";
                yi += 32;

                pnlNouvelleMarque.Top = yi;
                pnlNouvelleMarque.Visible = showNouvelleMarque;
                if (showNouvelleMarque) yi += pnlNouvelleMarque.Height + 8;

                txtReferenceModele.Top = yi; yi += 28;
                txtDesignationModele.Top = yi; yi += 28;
                txtNumeroModeleConstructeur.Top = yi; yi += 28;
                btnEnregistrerModele.Top = yi; yi += 34;

                pnlNouveauModele.Height = yi + 5;
                y += pnlNouveauModele.Height + 10;
            }
            else
            {
                pnlNouveauModele.Height = 0;
            }

            txtNumeroSerie.Top = y; y += 34;
            dtpDateAcquisition.Top = y; y += 34;
            cmbStatut.Top = y; y += 34;
            cmbEtat.Top = y; y += 34;

            bool aDejaUnBarcode = EnModeEdition && !string.IsNullOrEmpty(_codeBarreActuel);
            chkGenererBarcode.Visible = !aDejaUnBarcode;
            lblCodeBarreActuel.Visible = aDejaUnBarcode;

            if (aDejaUnBarcode)
            {
                lblCodeBarreActuel.Text = "Code-barre actuel : " + _codeBarreActuel;
                lblCodeBarreActuel.Top = y;
            }
            else
            {
                chkGenererBarcode.Top = y;
            }
            y += 34;

            btnEnregistrer.Top = y;
            btnEnregistrer.Left = ClientSize.Width - (EnModeEdition ? 300 : 220);
            btnAnnuler.Top = y;
            btnAnnuler.Left = ClientSize.Width - 110;
            y += 45;

            this.AutoScrollMinSize = new Size(0, y);
        }

        private void ChargerCategories()
        {
            var t = DatabaseHelper.ExecuteQuery("SELECT id, designation FROM Categorie ORDER BY designation");
            var ligneVide = t.NewRow();
            ligneVide["id"] = DBNull.Value;
            ligneVide["designation"] = "-- Aucune --";
            t.Rows.InsertAt(ligneVide, 0);

            cmbCategorie.DataSource = t;
            cmbCategorie.DisplayMember = "designation";
            cmbCategorie.ValueMember = "id";
            cmbCategorie.SelectedIndex = 0; // "-- Aucune --" par défaut
        }

        private void ChargerMarques()
        {
            var t = DatabaseHelper.ExecuteQuery("SELECT id, designation FROM Marque ORDER BY designation");
            var ligneVide = t.NewRow();
            ligneVide["id"] = DBNull.Value;
            ligneVide["designation"] = "-- Aucune --";
            t.Rows.InsertAt(ligneVide, 0);

            cmbMarque.DataSource = t;
            cmbMarque.DisplayMember = "designation";
            cmbMarque.ValueMember = "id";
            cmbMarque.SelectedIndex = 0; // "-- Aucune --" par défaut
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

        private void ChargerDonneesEquipement()
        {
            var t = DatabaseHelper.ExecuteQuery(
                "SELECT modele_id, numero_serie, statut, etat, date_acquisition, code_barre FROM Equipement WHERE id = @id",
                new SqliteParameter("@id", _equipementIdEnEdition!.Value));

            if (t.Rows.Count == 0)
            {
                MessageBox.Show("Équipement introuvable (peut-être déjà supprimé).", "Erreur",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close();
                return;
            }

            var row = t.Rows[0];
            cmbModele.SelectedValue = Convert.ToInt32(row["modele_id"]);
            txtNumeroSerie.Text = row["numero_serie"] == DBNull.Value ? "" : row["numero_serie"].ToString();
            cmbStatut.SelectedItem = row["statut"].ToString();
            cmbEtat.SelectedItem = row["etat"] == DBNull.Value ? "Bon" : row["etat"].ToString();

            if (DateTime.TryParse(row["date_acquisition"].ToString(), out var d))
                dtpDateAcquisition.Value = d;

            _codeBarreActuel = row["code_barre"] == DBNull.Value ? null : row["code_barre"].ToString();
            Relayout();
        }

        private void BtnEnregistrerCategorie_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtCodeCategorie.Text) || string.IsNullOrWhiteSpace(txtDesignationCategorie.Text))
            {
                MessageBox.Show("Code et désignation obligatoires.", "Champs manquants", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            try
            {
                string sql = "INSERT INTO Categorie (code, designation) VALUES (@code, @desig); SELECT last_insert_rowid();";
                using (var conn = DatabaseHelper.GetConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = sql;
                    cmd.Parameters.AddWithValue("@code", txtCodeCategorie.Text.Trim().ToUpper());
                    cmd.Parameters.AddWithValue("@desig", txtDesignationCategorie.Text.Trim());
                    var newId = Convert.ToInt32(cmd.ExecuteScalar());

                    ChargerCategories();
                    cmbCategorie.SelectedValue = newId;
                    showNouvelleCategorie = false;
                    txtCodeCategorie.Clear();
                    txtDesignationCategorie.Clear();
                    Relayout();
                }
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
            {
                MessageBox.Show("Ce code de catégorie existe déjà.", "Doublon", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnEnregistrerMarque_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtCodeMarque.Text) || string.IsNullOrWhiteSpace(txtDesignationMarque.Text))
            {
                MessageBox.Show("Code et désignation obligatoires.", "Champs manquants", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            try
            {
                string sql = "INSERT INTO Marque (code, designation) VALUES (@code, @desig); SELECT last_insert_rowid();";
                using (var conn = DatabaseHelper.GetConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = sql;
                    cmd.Parameters.AddWithValue("@code", txtCodeMarque.Text.Trim().ToUpper());
                    cmd.Parameters.AddWithValue("@desig", txtDesignationMarque.Text.Trim());
                    var newId = Convert.ToInt32(cmd.ExecuteScalar());

                    ChargerMarques();
                    cmbMarque.SelectedValue = newId;
                    showNouvelleMarque = false;
                    txtCodeMarque.Clear();
                    txtDesignationMarque.Clear();
                    Relayout();
                }
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
            {
                MessageBox.Show("Ce code de marque existe déjà.", "Doublon", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnEnregistrerModele_Click(object? sender, EventArgs e)
        {
            // Catégorie et Marque sont FACULTATIVES : le schéma DB les autorise
            // à NULL (ex: "Bureau", "Chaise" n'ont souvent ni marque connue ni
            // catégorie standardisée). Seuls référence et désignation sont
            // réellement obligatoires.
            if (string.IsNullOrWhiteSpace(txtReferenceModele.Text) || string.IsNullOrWhiteSpace(txtDesignationModele.Text))
            {
                MessageBox.Show("Référence et désignation du modèle obligatoires.", "Champs manquants", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            try
            {
                string sql = @"
                    INSERT INTO Modele (reference, designation, numero_modele, categorie_id, marque_id)
                    VALUES (@ref, @desig, @numMod, @catId, @marqId);
                    SELECT last_insert_rowid();";
                using (var conn = DatabaseHelper.GetConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = sql;
                    cmd.Parameters.AddWithValue("@ref", txtReferenceModele.Text.Trim());
                    cmd.Parameters.AddWithValue("@desig", txtDesignationModele.Text.Trim());
                    cmd.Parameters.AddWithValue("@numMod",
                        string.IsNullOrWhiteSpace(txtNumeroModeleConstructeur.Text) ? (object)DBNull.Value : txtNumeroModeleConstructeur.Text.Trim());

                    object catValue = (cmbCategorie.SelectedValue == null || cmbCategorie.SelectedValue == DBNull.Value)
                        ? DBNull.Value : cmbCategorie.SelectedValue;
                    object marqValue = (cmbMarque.SelectedValue == null || cmbMarque.SelectedValue == DBNull.Value)
                        ? DBNull.Value : cmbMarque.SelectedValue;

                    cmd.Parameters.AddWithValue("@catId", catValue);
                    cmd.Parameters.AddWithValue("@marqId", marqValue);
                    var newId = Convert.ToInt32(cmd.ExecuteScalar());

                    ChargerModeles();
                    cmbModele.SelectedValue = newId;
                    showNouveauModele = false;
                    txtReferenceModele.Clear();
                    txtDesignationModele.Clear();
                    txtNumeroModeleConstructeur.Clear();
                    Relayout();
                }
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
            {
                MessageBox.Show("Cette référence de modèle existe déjà.", "Doublon", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnEnregistrer_Click(object? sender, EventArgs e)
        {
            if (cmbModele.SelectedValue == null)
            {
                MessageBox.Show("Veuillez choisir un modèle, ou en créer un nouveau.", "Champ manquant", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            try
            {
                if (EnModeEdition)
                    EnregistrerModification();
                else
                    EnregistrerNouvelArticle();

                EquipementAjoute = true;
                _mainForm?.ChargerEquipements();
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
            {
                MessageBox.Show(
                    "Enregistrement refusé.\n\n" +
                    "Causes possibles :\n" +
                    "- Le modèle choisi n'a pas de marque/catégorie\n" +
                    "- Ce numéro de série existe déjà\n\n" +
                    "Détail : " + ex.Message,
                    "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur : " + ex.Message, "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void EnregistrerNouvelArticle()
        {
            string sql = @"
                INSERT INTO Equipement (modele_id, numero_serie, statut, etat, date_acquisition, barcode_genere)
                VALUES (@modeleId, @numSerie, @statut, @etat, @dateAcq, @barGen);";

            using (var conn = DatabaseHelper.GetConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = sql;
                cmd.Parameters.AddWithValue("@modeleId", cmbModele.SelectedValue);
                cmd.Parameters.AddWithValue("@numSerie",
                    string.IsNullOrWhiteSpace(txtNumeroSerie.Text) ? (object)DBNull.Value : txtNumeroSerie.Text.Trim());
                cmd.Parameters.AddWithValue("@statut", cmbStatut.SelectedItem?.ToString() ?? "En stock");
                cmd.Parameters.AddWithValue("@etat", cmbEtat.SelectedItem?.ToString() ?? "Bon");
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
                    date_acquisition = @dateAcq,
                    date_modification = CURRENT_TIMESTAMP" +
                    (demandeGeneration ? ", barcode_genere = 1" : "") + @"
                WHERE id = @id;";

            using (var conn = DatabaseHelper.GetConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = sql;
                cmd.Parameters.AddWithValue("@modeleId", cmbModele.SelectedValue);
                cmd.Parameters.AddWithValue("@numSerie",
                    string.IsNullOrWhiteSpace(txtNumeroSerie.Text) ? (object)DBNull.Value : txtNumeroSerie.Text.Trim());
                cmd.Parameters.AddWithValue("@statut", cmbStatut.SelectedItem?.ToString() ?? "En stock");
                cmd.Parameters.AddWithValue("@etat", cmbEtat.SelectedItem?.ToString() ?? "Bon");
                cmd.Parameters.AddWithValue("@dateAcq", dtpDateAcquisition.Value.ToString("yyyy-MM-dd"));
                cmd.Parameters.AddWithValue("@id", _equipementIdEnEdition!.Value);
                cmd.ExecuteNonQuery();
            }
        }
    }
}