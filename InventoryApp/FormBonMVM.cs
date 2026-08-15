using System;
using System.Data;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Microsoft.Data.Sqlite;
using Microsoft.VisualBasic;

namespace InventoryApp
{
    public partial class FormBonMVM : Form
    {
        // Connection string pointant vers le bon fichier de base de données
        private readonly string connectionString =
            $"Data Source={Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "gestion_equipement_2.db")};";

        private int currentMouvementId = 0; // Stocke l'ID du mouvement en cours

        public FormBonMVM()
        {
            InitializeComponent();

            // Abonnement aux événements du formulaire et des boutons
            this.Load += FormBonMVM_Load;

            if (this.Controls.ContainsKey("btnEnregistrer"))
            {
                var btn = this.Controls["btnEnregistrer"] as Button;
                if (btn != null) btn.Click += BtnEnregistrer_Click;
            }

            if (this.Controls.ContainsKey("btnAjouterLigne"))
            {
                var btn = this.Controls["btnAjouterLigne"] as Button;
                if (btn != null) btn.Click += BtnAjouterLigne_Click;
            }
        }

        private void FormBonMVM_Load(object sender, EventArgs e)
        {
            ChargerEmployes();
            ChargerEquipements();
            InitComboBoxEtat();
        }

        // 1. Chargement des Employés dans le ComboBox
        private void ChargerEmployes()
        {
            try
            {
                using (var conn = new SqliteConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT id, (nom || ' ' || prenom) AS nom_complet FROM Employe WHERE statut = 'Actif'";
                    using (var cmd = new SqliteCommand(query, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        DataTable dt = new DataTable();
                        dt.Load(reader);

                        if (this.Controls.ContainsKey("cmbEmploye"))
                        {
                            var cmb = this.Controls["cmbEmploye"] as ComboBox;
                            if (cmb != null)
                            {
                                cmb.DataSource = dt;
                                cmb.DisplayMember = "nom_complet";
                                cmb.ValueMember = "id";
                                cmb.SelectedIndex = -1;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des employés : {ex.Message}", "Erreur BDD", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // 2. Chargement des équipements dans le ComboBox
        private void ChargerEquipements()
        {
            try
            {
                using (var conn = new SqliteConnection(connectionString))
                {
                    conn.Open();
                    string query = @"SELECT eq.id, (md.designation || ' - SN: ' || COALESCE(eq.numero_serie, 'N/A')) AS display_name 
                                    FROM Equipement eq 
                                    JOIN Modele md ON eq.modele_id = md.id";
                    using (var cmd = new SqliteCommand(query, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        DataTable dt = new DataTable();
                        dt.Load(reader);

                        if (this.Controls.ContainsKey("cmbEquipement"))
                        {
                            var cmb = this.Controls["cmbEquipement"] as ComboBox;
                            if (cmb != null)
                            {
                                cmb.DataSource = dt;
                                cmb.DisplayMember = "display_name";
                                cmb.ValueMember = "id";
                                cmb.SelectedIndex = -1;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des équipements : {ex.Message}", "Erreur BDD", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // 3. Initialisation du ComboBox d'état de l'équipement
        private void InitComboBoxEtat()
        {
            if (this.Controls.ContainsKey("cmbEtat"))
            {
                var cmb = this.Controls["cmbEtat"] as ComboBox;
                if (cmb != null)
                {
                    cmb.DataSource = new[] { "Neuf", "Bon", "Usé", "Endommagé", "Hors service" };
                    cmb.SelectedIndex = 1; // "Bon" par défaut
                }
            }
        }

        // 4. Création/Mise à jour du Mouvement principal
        private void BtnEnregistrer_Click(object sender, EventArgs e)
        {
            try
            {
                var cmbEmploye = this.Controls.ContainsKey("cmbEmploye") ? this.Controls["cmbEmploye"] as ComboBox : null;
                var dtpDate = this.Controls.ContainsKey("dtpDate") ? this.Controls["dtpDate"] as DateTimePicker : null;
                var txtReference = this.Controls.ContainsKey("txtReference") ? this.Controls["txtReference"] as TextBox : null;
                var txtObservation = this.Controls.ContainsKey("txtObservation") ? this.Controls["txtObservation"] as TextBox : null;

                if (cmbEmploye == null || cmbEmploye.SelectedValue == null)
                {
                    MessageBox.Show("Veuillez sélectionner un employé.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                int employeId = Convert.ToInt32(cmbEmploye.SelectedValue);
                DateTime dateMvm = dtpDate != null ? dtpDate.Value : DateTime.Now;
                string reference = txtReference != null ? txtReference.Text : "";
                string observation = txtObservation != null ? txtObservation.Text : "";

                using (var conn = new SqliteConnection(connectionString))
                {
                    conn.Open();

                    if (currentMouvementId == 0)
                    {
                        // INSERT Mouvement principal
                        string insertSql = @"INSERT INTO Mouvement (code_mouvement, nom, reference, type_mouvement, employe_id, date_mouvement, observation)
                                             VALUES (@code, 'وصل استلام', @ref, 'Affectation', @employe, @date, @obs);
                                             SELECT last_insert_rowid();";

                        using (var cmd = new SqliteCommand(insertSql, conn))
                        {
                            cmd.Parameters.AddWithValue("@code", "MVT-" + DateTime.Now.ToString("yyyyMMddHHmmss"));
                            cmd.Parameters.AddWithValue("@ref", reference);
                            cmd.Parameters.AddWithValue("@employe", employeId);
                            cmd.Parameters.AddWithValue("@date", dateMvm.ToString("yyyy-MM-dd HH:mm:ss"));
                            cmd.Parameters.AddWithValue("@obs", observation);

                            currentMouvementId = Convert.ToInt32(cmd.ExecuteScalar());
                        }

                        MessageBox.Show("Mouvement créé avec succès ! Vous pouvez maintenant ajouter des lignes d'équipement.", "Succès", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        // UPDATE Mouvement principal
                        string updateSql = @"UPDATE Mouvement
                                             SET employe_id = @employe,
                                                 reference = @ref,
                                                 date_mouvement = @date,
                                                 observation = @obs
                                             WHERE id = @id;";
                        using (var cmd = new SqliteCommand(updateSql, conn))
                        {
                            cmd.Parameters.AddWithValue("@employe", employeId);
                            cmd.Parameters.AddWithValue("@ref", reference);
                            cmd.Parameters.AddWithValue("@date", dateMvm.ToString("yyyy-MM-dd HH:mm:ss"));
                            cmd.Parameters.AddWithValue("@obs", observation);
                            cmd.Parameters.AddWithValue("@id", currentMouvementId);

                            cmd.ExecuteNonQuery();
                            MessageBox.Show("Mouvement mis à jour avec succès.", "Succès", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'enregistrement du mouvement : {ex.Message}", "Erreur BDD", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // 5. Ajout d'une ligne d'équipement dans Ligne_mouvement
        private void BtnAjouterLigne_Click(object sender, EventArgs e)
        {
            if (currentMouvementId == 0)
            {
                MessageBox.Show("Veuillez d'abord enregistrer le mouvement principal.", "Attention", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var cmbEquipement = this.Controls.ContainsKey("cmbEquipement") ? this.Controls["cmbEquipement"] as ComboBox : null;
            var cmbEtat = this.Controls.ContainsKey("cmbEtat") ? this.Controls["cmbEtat"] as ComboBox : null;
            var chkEstSortie = this.Controls.ContainsKey("chkEstSortie") ? this.Controls["chkEstSortie"] as CheckBox : null;

            if (cmbEquipement == null || cmbEquipement.SelectedValue == null)
            {
                MessageBox.Show("Veuillez sélectionner un équipement.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                int equipementId = Convert.ToInt32(cmbEquipement.SelectedValue);
                string etat = cmbEtat != null ? cmbEtat.Text : "Bon";
                int estSortie = (chkEstSortie != null && chkEstSortie.Checked) ? 1 : 0;

                using (var conn = new SqliteConnection(connectionString))
                {
                    conn.Open();
                    string query = @"INSERT INTO Ligne_mouvement (mouvement_id, equipement_id, etat_a_la_mouvement, est_sortie) 
                                    VALUES (@mvtId, @eqId, @etat, @estSortie)";

                    using (var cmd = new SqliteCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@mvtId", currentMouvementId);
                        cmd.Parameters.AddWithValue("@eqId", equipementId);
                        cmd.Parameters.AddWithValue("@etat", etat);
                        cmd.Parameters.AddWithValue("@estSortie", estSortie);

                        cmd.ExecuteNonQuery();
                    }
                }

                ChargerLignesMouvement(); // Rafraîchir le DataGridView
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'ajout de la ligne : {ex.Message}", "Erreur SQLite", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // 6. Charger et afficher les lignes dans le DataGridView
        private void ChargerLignesMouvement()
        {
            if (currentMouvementId == 0) return;

            try
            {
                using (var conn = new SqliteConnection(connectionString))
                {
                    conn.Open();
                    string query = @"SELECT lm.id, md.designation AS Equipement, eq.numero_serie AS [N° Série], 
                                           lm.etat_a_la_mouvement AS Etat, 
                                           CASE WHEN lm.est_sortie = 1 THEN 'Oui' ELSE 'Non' END AS [Est Sortie]
                                    FROM Ligne_mouvement lm
                                    JOIN Equipement eq ON lm.equipement_id = eq.id
                                    JOIN Modele md ON eq.modele_id = md.id
                                    WHERE lm.mouvement_id = @mvtId";

                    using (var cmd = new SqliteCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@mvtId", currentMouvementId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            DataTable dt = new DataTable();
                            dt.Load(reader);

                            if (this.Controls.ContainsKey("dgvLignesMouvement"))
                            {
                                var dgv = this.Controls["dgvLignesMouvement"] as DataGridView;
                                if (dgv != null) dgv.DataSource = dt;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement de la grille : {ex.Message}", "Erreur BDD", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Réinitialisation du formulaire
        private void ResetForm()
        {
            currentMouvementId = 0;
            ChargerEmployes();
            ChargerEquipements();
            InitComboBoxEtat();

            if (this.Controls.ContainsKey("dgvLignesMouvement"))
            {
                var dgv = this.Controls["dgvLignesMouvement"] as DataGridView;
                if (dgv != null) dgv.DataSource = null;
            }
        }
    }
}