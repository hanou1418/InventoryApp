using System;
using System.Data;
using System.Windows.Forms;
using InventoryApp.Data;
using Microsoft.Data.Sqlite;

namespace InventoryApp
{
    public class FrmAjouterEquipement : Form
    {
        // true si un equipement a bien ete ajoute -> le formulaire appelant
        // saura qu'il doit rafraichir sa grille.
        private Form1 _mainForm;
        public bool EquipementAjoute { get; private set; } = false;

        private ComboBox cmbModele;
        private Button btnNouveauModele;
        private TextBox txtNumeroSerie;
        private DateTimePicker dtpDateAcquisition;
        private ComboBox cmbStatut;
        private CheckBox chkGenererBarcode;
        private Button btnEnregistrer;
        private Button btnAnnuler;

        public FrmAjouterEquipement(Form1 mainForm)
        {
            _mainForm = mainForm;
            Text = "Ajouter un article";
            Width = 460;
            Height = 380;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            var lblModele = new Label { Text = "Modèle *", Left = 20, Top = 20, Width = 150 };
            cmbModele = new ComboBox { Left = 20, Top = 45, Width = 300, DropDownStyle = ComboBoxStyle.DropDownList };
            btnNouveauModele = new Button { Text = "+ Nouveau", Left = 330, Top = 44, Width = 90 };
            btnNouveauModele.Click += BtnNouveauModele_Click;

            var lblNumeroSerie = new Label { Text = "N° série (optionnel)", Left = 20, Top = 80, Width = 200 };
            txtNumeroSerie = new TextBox { Left = 20, Top = 105, Width = 400 };

            var lblDateAcquisition = new Label { Text = "Date d'acquisition", Left = 20, Top = 140, Width = 250 };
            dtpDateAcquisition = new DateTimePicker
            {
                Left = 20,
                Top = 165,
                Width = 200,
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Today
            };

            var lblStatut = new Label { Text = "Statut", Left = 20, Top = 200, Width = 150 };
            cmbStatut = new ComboBox { Left = 20, Top = 225, Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbStatut.Items.AddRange(new object[] { "En stock", "Affecté", "En prêt", "En panne", "En réparation", "Réformé" });
            cmbStatut.SelectedIndex = 0;

            chkGenererBarcode = new CheckBox
            {
                Text = "Générer le code-barre immédiatement",
                Left = 20,
                Top = 265,
                Width = 380
            };

            btnEnregistrer = new Button { Text = "Enregistrer", Left = 230, Top = 300, Width = 90 };
            btnAnnuler = new Button { Text = "Annuler", Left = 330, Top = 300, Width = 90 };
            btnEnregistrer.Click += BtnEnregistrer_Click;
            btnAnnuler.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            Controls.AddRange(new Control[]
            {
                lblModele, cmbModele, btnNouveauModele,
                lblNumeroSerie, txtNumeroSerie,
                lblDateAcquisition, dtpDateAcquisition,
                lblStatut, cmbStatut,
                chkGenererBarcode,
                btnEnregistrer, btnAnnuler
            });

            Load += (s, e) => ChargerModeles();
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

            DataTable modeles = DatabaseHelper.ExecuteQuery(sql);
            cmbModele.DataSource = modeles;
            cmbModele.DisplayMember = "affichage";
            cmbModele.ValueMember = "id";
        }

        private void BtnNouveauModele_Click(object sender, EventArgs e)
        {
            using (var frm = new FrmAjouterArticle())
            {
                if (frm.ShowDialog(this) == DialogResult.OK && frm.EquipementAjoute)
                {
                    _mainForm?.ChargerEquipements();
                }
            }
        }

        private void BtnEnregistrer_Click(object sender, EventArgs e)
        {
            if (cmbModele.SelectedValue == null)
            {
                MessageBox.Show("Veuillez choisir un modèle, ou en créer un nouveau.", "Champ manquant",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                string sql = @"
                    INSERT INTO Equipement (modele_id, numero_serie, statut, date_acquisition, barcode_genere)
                    VALUES (@modeleId, @numSerie, @statut, @dateAcq, @barGen);";

                using (var conn = DatabaseHelper.GetConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = sql;
                    cmd.Parameters.AddWithValue("@modeleId", cmbModele.SelectedValue);
                    cmd.Parameters.AddWithValue("@numSerie",
                        string.IsNullOrWhiteSpace(txtNumeroSerie.Text) ? (object)DBNull.Value : txtNumeroSerie.Text.Trim());
                    cmd.Parameters.AddWithValue("@statut", cmbStatut.SelectedItem.ToString());
                    cmd.Parameters.AddWithValue("@dateAcq", dtpDateAcquisition.Value.ToString("yyyy-MM-dd"));
                    cmd.Parameters.AddWithValue("@barGen", chkGenererBarcode.Checked ? 1 : 0);

                    cmd.ExecuteNonQuery();
                }

                EquipementAjoute = true;
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
            {
                // Le trigger de generation refuse (marque/categorie manquante sur le modele choisi),
                // OU un vrai doublon de numero_serie.
                MessageBox.Show(
                    "Enregistrement refusé.\n\n" +
                    "Causes possibles :\n" +
                    "- Le modèle choisi n'a pas de marque/catégorie définie (décochez la génération du code-barre)\n" +
                    "- Ce numéro de série existe déjà\n\n" +
                    "Détail technique : " + ex.Message,
                    "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur lors de l'enregistrement : " + ex.Message, "Erreur",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}