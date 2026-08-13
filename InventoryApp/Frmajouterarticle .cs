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
    public class FrmAjouterArticle : Form
    {
        public bool EquipementAjoute { get; private set; } = false;
        private readonly Form1? _mainForm;

        private readonly int? _equipementIdEnEdition;
        private bool EnModeEdition => _equipementIdEnEdition.HasValue;

        private string? _codeBarreActuel = null;

        // --- Composants Guna UI ---
        private Guna2BorderlessForm borderlessForm = null!;
        private Guna2Panel pnlHeader = null!;
        private Label lblHeaderTitle = null!;
        private Guna2ControlBox btnCloseHeader = null!;

        private Guna2ComboBox cmbModele = null!;
        private Guna2Button btnToggleNouveauModele = null!;

        private Guna2Panel pnlNouveauModele = null!;
        private Guna2ComboBox cmbCategorie = null!;
        private Guna2Button btnToggleNouvelleCategorie = null!;
        private Guna2Panel pnlNouvelleCategorie = null!;
        private Guna2TextBox txtCodeCategorie = null!;
        private Guna2TextBox txtDesignationCategorie = null!;
        private Guna2Button btnEnregistrerCategorie = null!;

        private Guna2ComboBox cmbMarque = null!;
        private Guna2Button btnToggleNouvelleMarque = null!;
        private Guna2Panel pnlNouvelleMarque = null!;
        private Guna2TextBox txtCodeMarque = null!;
        private Guna2TextBox txtDesignationMarque = null!;
        private Guna2Button btnEnregistrerMarque = null!;

        private Guna2TextBox txtReferenceModele = null!;
        private Guna2TextBox txtDesignationModele = null!;
        private Guna2TextBox txtNumeroModeleConstructeur = null!;
        private Guna2Button btnEnregistrerModele = null!;

        private Guna2TextBox txtNumeroSerie = null!;
        private Guna2DateTimePicker dtpDateAcquisition = null!;
        private Guna2ComboBox cmbStatut = null!;
        private Guna2ComboBox cmbEtat = null!;
        private Guna2CheckBox chkGenererBarcode = null!;
        private Label lblCodeBarreActuel = null!;

        private Guna2Button btnEnregistrer = null!;
        private Guna2Button btnAnnuler = null!;

        private bool showNouveauModele = false;
        private bool showNouvelleCategorie = false;
        private bool showNouvelleMarque = false;

        private const int MARGE = 20;
        private const int LARGEUR_CHAMP = 400;

        public FrmAjouterArticle() : this(null, null) { }

        public FrmAjouterArticle(Form1? mainForm) : this(mainForm, null) { }

        public FrmAjouterArticle(Form1? mainForm, int? equipementIdEnEdition)
        {
            _mainForm = mainForm;
            _equipementIdEnEdition = equipementIdEnEdition;

            // Paramètres de la fenêtre sans bordure
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.FromArgb(248, 250, 252); // Gris/Blanc médical très élégant
            Width = 445;
            Height = 400;

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
            // Arrondis et ombre de la fenêtre
            borderlessForm = new Guna2BorderlessForm
            {
                ContainerControl = this,
                BorderRadius = 14,
                DragForm = true,
                HasFormShadow = true
            };

            // En-tête personnalisé
            pnlHeader = new Guna2Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                FillColor = Color.FromArgb(30, 41, 59), // Dark Slate
            };

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

            // Modèle & Bouton Toggle
            cmbModele = new Guna2ComboBox
            {
                Left = MARGE,
                Width = 275,
                Height = 36,
                BorderRadius = 6,
                Font = new Font("Segoe UI", 9.5F),
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            btnToggleNouveauModele = new Guna2Button
            {
                Left = MARGE + 285,
                Width = 115,
                Height = 36,
                Text = "+ Nouveau",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                BorderRadius = 6,
                FillColor = Color.FromArgb(59, 130, 246),
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            btnToggleNouveauModele.Click += (s, e) => { showNouveauModele = !showNouveauModele; Relayout(); };

            // Panel de création de Modèle
            pnlNouveauModele = new Guna2Panel
            {
                Left = MARGE,
                Width = LARGEUR_CHAMP,
                BorderRadius = 8,
                BorderColor = Color.FromArgb(203, 213, 225),
                BorderThickness = 1,
                FillColor = Color.White
            };

            cmbCategorie = new Guna2ComboBox { Left = 10, Width = 250, Height = 34, BorderRadius = 6, DropDownStyle = ComboBoxStyle.DropDownList };
            btnToggleNouvelleCategorie = new Guna2Button { Left = 270, Width = 115, Height = 34, Text = "+ Nouvelle", BorderRadius = 6, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold) };
            btnToggleNouvelleCategorie.Click += (s, e) => { showNouvelleCategorie = !showNouvelleCategorie; Relayout(); };

            pnlNouvelleCategorie = new Guna2Panel { Left = 10, Width = 375, Height = 46, BorderRadius = 6, FillColor = Color.FromArgb(241, 245, 249) };
            txtCodeCategorie = new Guna2TextBox { Left = 8, Top = 7, Width = 100, Height = 32, BorderRadius = 6, PlaceholderText = "Code (ex: MOB)" };
            txtDesignationCategorie = new Guna2TextBox { Left = 114, Top = 7, Width = 160, Height = 32, BorderRadius = 6, PlaceholderText = "Désignation" };
            btnEnregistrerCategorie = new Guna2Button { Left = 280, Top = 7, Width = 85, Height = 32, BorderRadius = 6, Text = "Créer", FillColor = Color.FromArgb(16, 185, 129) };
            btnEnregistrerCategorie.Click += BtnEnregistrerCategorie_Click;
            pnlNouvelleCategorie.Controls.AddRange(new Control[] { txtCodeCategorie, txtDesignationCategorie, btnEnregistrerCategorie });

            cmbMarque = new Guna2ComboBox { Left = 10, Width = 250, Height = 34, BorderRadius = 6, DropDownStyle = ComboBoxStyle.DropDownList };
            btnToggleNouvelleMarque = new Guna2Button { Left = 270, Width = 115, Height = 34, Text = "+ Nouvelle", BorderRadius = 6, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold) };
            btnToggleNouvelleMarque.Click += (s, e) => { showNouvelleMarque = !showNouvelleMarque; Relayout(); };

            pnlNouvelleMarque = new Guna2Panel { Left = 10, Width = 375, Height = 46, BorderRadius = 6, FillColor = Color.FromArgb(241, 245, 249) };
            txtCodeMarque = new Guna2TextBox { Left = 8, Top = 7, Width = 100, Height = 32, BorderRadius = 6, PlaceholderText = "Code (ex: HP)" };
            txtDesignationMarque = new Guna2TextBox { Left = 114, Top = 7, Width = 160, Height = 32, BorderRadius = 6, PlaceholderText = "Désignation" };
            btnEnregistrerMarque = new Guna2Button { Left = 280, Top = 7, Width = 85, Height = 32, BorderRadius = 6, Text = "Créer", FillColor = Color.FromArgb(16, 185, 129) };
            btnEnregistrerMarque.Click += BtnEnregistrerMarque_Click;
            pnlNouvelleMarque.Controls.AddRange(new Control[] { txtCodeMarque, txtDesignationMarque, btnEnregistrerMarque });

            txtReferenceModele = new Guna2TextBox { Left = 10, Width = 375, Height = 34, BorderRadius = 6, PlaceholderText = "Référence interne (unique)" };
            txtDesignationModele = new Guna2TextBox { Left = 10, Width = 375, Height = 34, BorderRadius = 6, PlaceholderText = "Désignation (ex: HP LaserJet 1020)" };
            txtNumeroModeleConstructeur = new Guna2TextBox { Left = 10, Width = 375, Height = 34, BorderRadius = 6, PlaceholderText = "N° modèle constructeur (optionnel)" };

            btnEnregistrerModele = new Guna2Button
            {
                Left = 235,
                Width = 150,
                Height = 36,
                Text = "Créer le modèle",
                BorderRadius = 6,
                FillColor = Color.FromArgb(16, 185, 129),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            btnEnregistrerModele.Click += BtnEnregistrerModele_Click;

            pnlNouveauModele.Controls.AddRange(new Control[]
            {
                cmbCategorie, btnToggleNouvelleCategorie, pnlNouvelleCategorie,
                cmbMarque, btnToggleNouvelleMarque, pnlNouvelleMarque,
                txtReferenceModele, txtDesignationModele, txtNumeroModeleConstructeur,
                btnEnregistrerModele
            });

            // Numéro de Série
            txtNumeroSerie = new Guna2TextBox
            {
                Left = MARGE,
                Width = LARGEUR_CHAMP,
                Height = 36,
                BorderRadius = 6,
                Font = new Font("Segoe UI", 9.5F),
                PlaceholderText = "N° série (optionnel)"
            };

            // Date d'acquisition
            dtpDateAcquisition = new Guna2DateTimePicker
            {
                Left = MARGE,
                Width = LARGEUR_CHAMP,
                Height = 36,
                BorderRadius = 6,
                Font = new Font("Segoe UI", 9F),
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Today,
                FillColor = Color.White,
                BorderColor = Color.FromArgb(213, 218, 223),
                BorderThickness = 1
            };

            // Statut
            cmbStatut = new Guna2ComboBox
            {
                Left = MARGE,
                Width = LARGEUR_CHAMP,
                Height = 36,
                BorderRadius = 6,
                Font = new Font("Segoe UI", 9.5F),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbStatut.Items.AddRange(new object[] { "En stock", "Affecté", "En prêt", "En panne", "En réparation", "Réformé" });
            cmbStatut.SelectedIndex = 0;

            // État
            cmbEtat = new Guna2ComboBox
            {
                Left = MARGE,
                Width = LARGEUR_CHAMP,
                Height = 36,
                BorderRadius = 6,
                Font = new Font("Segoe UI", 9.5F),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbEtat.Items.AddRange(new object[] { "Neuf", "Bon", "Usé", "Endommagé", "Hors service" });
            cmbEtat.SelectedIndex = 1;

            // Barcode Checkbox & Label
            chkGenererBarcode = new Guna2CheckBox
            {
                Text = "Générer le code-barre immédiatement",
                Left = MARGE,
                Width = LARGEUR_CHAMP,
                Font = new Font("Segoe UI", 9F),
                AutoSize = true,
                CheckedState = { FillColor = Color.FromArgb(59, 130, 246) }
            };

            lblCodeBarreActuel = new Label
            {
                Left = MARGE,
                Width = LARGEUR_CHAMP,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(16, 185, 129),
                Visible = false
            };

            // Boutons d'Action
            btnEnregistrer = new Guna2Button
            {
                Text = EnModeEdition ? "Enregistrer les modifications" : "Enregistrer",
                Height = 38,
                FillColor = Color.FromArgb(16, 185, 129), // Vert Emerald
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                BorderRadius = 8,
                Cursor = Cursors.Hand
            };

            btnAnnuler = new Guna2Button
            {
                Text = "Annuler",
                Width = 100,
                Height = 38,
                FillColor = Color.FromArgb(239, 68, 68), // Rouge Corail
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                BorderRadius = 8,
                Cursor = Cursors.Hand
            };

            btnEnregistrer.Click += BtnEnregistrer_Click;
            btnAnnuler.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            Controls.AddRange(new Control[]
            {
                pnlHeader,
                cmbModele, btnToggleNouveauModele, pnlNouveauModele,
                txtNumeroSerie, dtpDateAcquisition, cmbStatut, cmbEtat,
                chkGenererBarcode, lblCodeBarreActuel,
                btnEnregistrer, btnAnnuler
            });
        }

        private void Relayout()
        {
            int y = 65; // Espace après l'en-tête

            cmbModele.Top = y;
            btnToggleNouveauModele.Top = y;
            btnToggleNouveauModele.Text = showNouveauModele ? "- Fermer" : "+ Nouveau";
            y += 44;

            pnlNouveauModele.Top = y;
            pnlNouveauModele.Visible = showNouveauModele;

            if (showNouveauModele)
            {
                int yi = 12;
                cmbCategorie.Top = yi;
                btnToggleNouvelleCategorie.Top = yi;
                btnToggleNouvelleCategorie.Text = showNouvelleCategorie ? "- Fermer" : "+ Nouvelle";
                yi += 40;

                pnlNouvelleCategorie.Top = yi;
                pnlNouvelleCategorie.Visible = showNouvelleCategorie;
                if (showNouvelleCategorie) yi += pnlNouvelleCategorie.Height + 8;

                cmbMarque.Top = yi;
                btnToggleNouvelleMarque.Top = yi;
                btnToggleNouvelleMarque.Text = showNouvelleMarque ? "- Fermer" : "+ Nouvelle";
                yi += 40;

                pnlNouvelleMarque.Top = yi;
                pnlNouvelleMarque.Visible = showNouvelleMarque;
                if (showNouvelleMarque) yi += pnlNouvelleMarque.Height + 8;

                txtReferenceModele.Top = yi; yi += 40;
                txtDesignationModele.Top = yi; yi += 40;
                txtNumeroModeleConstructeur.Top = yi; yi += 40;
                btnEnregistrerModele.Top = yi; yi += 44;

                pnlNouveauModele.Height = yi + 8;
                y += pnlNouveauModele.Height + 12;
            }
            else
            {
                pnlNouveauModele.Height = 0;
            }

            txtNumeroSerie.Top = y; y += 44;
            dtpDateAcquisition.Top = y; y += 44;
            cmbStatut.Top = y; y += 44;
            cmbEtat.Top = y; y += 44;

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
            y += 38;

            // Dimensions et alignement dynamique des boutons
            int largeurBtnEnregistrer = EnModeEdition ? 210 : 120;
            btnEnregistrer.Width = largeurBtnEnregistrer;

            btnAnnuler.Top = y;
            btnAnnuler.Left = MARGE + LARGEUR_CHAMP - (btnAnnuler.Width + btnEnregistrer.Width + 10);

            btnEnregistrer.Top = y;
            btnEnregistrer.Left = MARGE + LARGEUR_CHAMP - btnEnregistrer.Width;

            y += btnEnregistrer.Height + 20;

            // Redimensionnement automatique de la fenêtre globale
            this.Height = y;
        }

        // --- Logique Métier & Chargement Données ---

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
            cmbCategorie.SelectedIndex = 0;
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
            cmbMarque.SelectedIndex = 0;
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