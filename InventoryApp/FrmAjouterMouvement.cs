#nullable enable
using Guna.UI2.WinForms;
using InventoryApp.Data;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace InventoryApp
{
    /// <summary>
    /// Formulaire popup Guna.UI2 pour créer ou modifier un Mouvement complet avec ses lignes.
    /// Écriture atomique en base via transaction SQL à la validation.
    /// </summary>
    public class FrmAjouterMouvement : Form
    {
        // Palette de couleurs personnalisée
        private readonly Color _primaryBlue = Color.FromArgb(37, 99, 235);
        private readonly Color _darkNavy = Color.FromArgb(24, 30, 54);
        private readonly Color _lightGray = Color.FromArgb(240, 242, 245);

        public bool MouvementEnregistre { get; private set; } = false;

        private readonly Form1? _mainForm;
        private readonly long? _mouvementIdToEdit; // Stocke l'ID si on est en mode modification
        private readonly BindingList<LigneMouvementTemp> _lignes = new BindingList<LigneMouvementTemp>();

        private Guna2ComboBox cmbEmploye = null!;
        private Guna2Button btnNouvelEmploye = null!;
        private Guna2ComboBox cmbNomMouvement = null!;
        private Guna2ComboBox cmbTypeMouvement = null!;
        private Guna2TextBox txtReference = null!;
        private Guna2DateTimePicker dtpDateMouvement = null!;
        private Guna2TextBox txtContenu = null!;
        private Guna2TextBox txtObservationGenerale = null!;

        private Guna2DataGridView dgvLignes = null!;
        private Guna2Button btnAjouterLigne = null!;
        private Guna2Button btnEnregistrer = null!;
        private Guna2Button btnAnnuler = null!;
        private Label lblTitre = null!;

        // 1. Constructeur pour la CRÉATION (1 argument)
        public FrmAjouterMouvement(Form1? mainForm) : this(mainForm, null)
        {
        }

        // 2. Constructeur pour la MODIFICATION (2 arguments)
        public FrmAjouterMouvement(Form1? mainForm, long? mouvementId)
        {
            _mainForm = mainForm;
            _mouvementIdToEdit = mouvementId;

            Text = _mouvementIdToEdit.HasValue ? "Modifier le mouvement" : "Nouveau mouvement";
            Size = new Size(820, 720);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.None;
            BackColor = Color.White;

            ConstruireControles();

            Load += (s, e) =>
            {
                ChargerEmployes();
                if (_mouvementIdToEdit.HasValue)
                {
                    ChargerMouvementExistant(_mouvementIdToEdit.Value);
                }
                RafraichirGrille();
            };
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

            lblTitre = new Label
            {
                Text = _mouvementIdToEdit.HasValue ? "MODIFIER LE MOUVEMENT DE STOCK" : "NOUVEAU MOUVEMENT DE STOCK",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            panelHeader.Controls.Add(lblTitre);
            Controls.Add(panelHeader);

            const int margeG = 25, margeD = 420, largeurChamp = 370;
            int y = 70;

            Label MakeLabel(string texte, int left)
            {
                var l = new Label
                {
                    Text = texte,
                    Left = left,
                    Top = y,
                    Width = largeurChamp,
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                    ForeColor = _darkNavy
                };
                Controls.Add(l);
                return l;
            }

            // ---- Colonne gauche ----
            MakeLabel("Employé", margeG);
            cmbEmploye = new Guna2ComboBox
            {
                Left = margeG,
                Top = y + 20,
                Width = largeurChamp - 110,
                Height = 36,
                BorderRadius = 6,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            btnNouvelEmploye = new Guna2Button
            {
                Text = "+ Nouveau",
                Left = margeG + largeurChamp - 100,
                Top = y + 20,
                Width = 100,
                Height = 36,
                BorderRadius = 6,
                FillColor = _primaryBlue,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnNouvelEmploye.Click += BtnNouvelEmploye_Click;
            Controls.Add(cmbEmploye);
            Controls.Add(btnNouvelEmploye);

            // ---- Colonne droite ----
            MakeLabel("Type de document", margeD);
            cmbNomMouvement = new Guna2ComboBox
            {
                Left = margeD,
                Top = y + 20,
                Width = largeurChamp,
                Height = 36,
                BorderRadius = 6,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbNomMouvement.Items.AddRange(new object[] { "مخالصة", "وصل استلام" });
            cmbNomMouvement.SelectedIndex = 1;
            Controls.Add(cmbNomMouvement);

            y += 65;

            MakeLabel("Type de mouvement *", margeG);
            cmbTypeMouvement = new Guna2ComboBox
            {
                Left = margeG,
                Top = y + 20,
                Width = largeurChamp,
                Height = 36,
                BorderRadius = 6,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbTypeMouvement.Items.AddRange(new object[] { "Affectation", "Prêt", "Retour", "Maintenance", "Réforme" });
            cmbTypeMouvement.SelectedIndex = 0;
            Controls.Add(cmbTypeMouvement);

            MakeLabel("Date", margeD);
            dtpDateMouvement = new Guna2DateTimePicker
            {
                Left = margeD,
                Top = y + 20,
                Width = largeurChamp,
                Height = 36,
                BorderRadius = 6,
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Today,
                FillColor = _lightGray,
                ForeColor = _darkNavy
            };
            Controls.Add(dtpDateMouvement);

            y += 65;

            MakeLabel("Référence (optionnel)", margeG);
            txtReference = new Guna2TextBox
            {
                Left = margeG,
                Top = y + 20,
                Width = largeurChamp,
                Height = 36,
                BorderRadius = 6
            };
            Controls.Add(txtReference);

            MakeLabel("Observation générale (optionnel)", margeD);
            txtObservationGenerale = new Guna2TextBox
            {
                Left = margeD,
                Top = y + 20,
                Width = largeurChamp,
                Height = 36,
                BorderRadius = 6
            };
            Controls.Add(txtObservationGenerale);

            y += 65;

            MakeLabel("Contenu du document (Texte d'attestation)", margeG);
            txtContenu = new Guna2TextBox
            {
                Left = margeG,
                Top = y + 20,
                Width = margeD + largeurChamp - margeG,
                Height = 55,
                Multiline = true,
                BorderRadius = 6,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                Text = "أصرح باني استلمت من السيد(ة) المكلف(ة) بتسيير مكتب الوسائل العامة والمخزن بمديرية المواصلات السلكية واللاسلكية، العتاد المبيّن في الجدول أدناه:"
            };
            Controls.Add(txtContenu);

            y += 85;

            // Section Grille
            var lblLignes = new Label
            {
                Text = "LIGNES DU MOUVEMENT",
                Left = margeG,
                Top = y + 5,
                Width = 250,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = _darkNavy
            };
            Controls.Add(lblLignes);

            btnAjouterLigne = new Guna2Button
            {
                Text = "+ Ajouter une ligne",
                Left = margeD + largeurChamp - 170,
                Top = y,
                Width = 170,
                Height = 36,
                BorderRadius = 6,
                FillColor = _primaryBlue,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnAjouterLigne.Click += BtnAjouterLigne_Click;
            Controls.Add(btnAjouterLigne);

            y += 45;

            dgvLignes = new Guna2DataGridView
            {
                Left = margeG,
                Top = y,
                Width = margeD + largeurChamp - margeG,
                Height = 200,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                AutoGenerateColumns = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            dgvLignes.ThemeStyle.HeaderStyle.BackColor = _darkNavy;
            dgvLignes.ThemeStyle.HeaderStyle.ForeColor = Color.White;
            dgvLignes.ThemeStyle.HeaderStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            dgvLignes.ThemeStyle.RowsStyle.SelectionBackColor = Color.FromArgb(220, 235, 252);
            dgvLignes.ThemeStyle.RowsStyle.SelectionForeColor = _darkNavy;

            dgvLignes.Columns.Add(new DataGridViewTextBoxColumn { Name = "colAffichage", HeaderText = "Équipement", DataPropertyName = "Affichage", Width = 260 });
            dgvLignes.Columns.Add(new DataGridViewTextBoxColumn { Name = "colEtat", HeaderText = "État", DataPropertyName = "Etat", Width = 80 });
            dgvLignes.Columns.Add(new DataGridViewCheckBoxColumn { Name = "colSortie", HeaderText = "Sortie ?", DataPropertyName = "EstSortie", Width = 60 });
            dgvLignes.Columns.Add(new DataGridViewTextBoxColumn { Name = "colObs", HeaderText = "Observation", DataPropertyName = "Observation", Width = 140 });
            dgvLignes.Columns.Add(new DataGridViewTextBoxColumn { Name = "colModifierLigne", HeaderText = "Modifier", Width = 70, ReadOnly = true });
            dgvLignes.Columns.Add(new DataGridViewTextBoxColumn { Name = "colSupprimerLigne", HeaderText = "Supprimer", Width = 75, ReadOnly = true });
            // Événements pour le clic et le dessin personnalisé des boutons d'actions
            dgvLignes.CellMouseClick += DgvLignes_CellMouseClick;
            dgvLignes.CellPainting += DgvLignes_CellPainting;

            // Rafraîchir la grille pour animer les boutons au survol
            dgvLignes.MouseMove += (s, e) => dgvLignes.Invalidate();
            dgvLignes.MouseDown += (s, e) => dgvLignes.Invalidate();
            dgvLignes.MouseUp += (s, e) => dgvLignes.Invalidate();

            dgvLignes.DataSource = _lignes;
            Controls.Add(dgvLignes);

            y += 215;

            // Boutons de validation
            btnEnregistrer = new Guna2Button
            {
                Text = _mouvementIdToEdit.HasValue ? "Mettre à jour" : "Enregistrer le mouvement",
                Left = margeD + largeurChamp - 320,
                Top = y,
                Width = 210,
                Height = 42,
                BorderRadius = 6,
                FillColor = _primaryBlue,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };

            btnAnnuler = new Guna2Button
            {
                Text = "Annuler",
                Left = margeD + largeurChamp - 100,
                Top = y,
                Width = 100,
                Height = 42,
                BorderRadius = 6,
                FillColor = _lightGray,
                ForeColor = _darkNavy,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };

            btnEnregistrer.Click += BtnEnregistrer_Click;
            btnAnnuler.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            Controls.Add(btnEnregistrer);
            Controls.Add(btnAnnuler);
        }

        private void DgvLignes_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.Graphics == null) return;

            Point mousePos = dgvLignes.PointToClient(Cursor.Position);
            bool isHovered = e.CellBounds.Contains(mousePos);
            bool isClicked = isHovered && (Control.MouseButtons == MouseButtons.Left);
            int iconSize = 18;

            string colName = dgvLignes.Columns[e.ColumnIndex].Name;

            if (colName == "colModifierLigne")
            {
                DessinerBoutonAction(e, isHovered, isClicked,
                    Color.FromArgb(240, 253, 244), Color.FromArgb(220, 252, 231), Color.FromArgb(187, 247, 208),
                    Color.FromArgb(134, 239, 172), "pencil_icon.png", iconSize);
            }
            else if (colName == "colSupprimerLigne")
            {
                DessinerBoutonAction(e, isHovered, isClicked,
                    Color.FromArgb(254, 242, 242), Color.FromArgb(254, 226, 226), Color.FromArgb(254, 202, 202),
                    Color.FromArgb(252, 165, 165), "delet_icon.png", iconSize);
            }
        }

        private void DessinerBoutonAction(DataGridViewCellPaintingEventArgs e, bool isHovered, bool isClicked,
            Color bg, Color bgHover, Color bgClick, Color borderColor, string iconFilename, int iconSize)
        {
            if (e.Graphics == null) return;

            // 1. Dessiner d'abord le fond standard de la cellule
            e.PaintBackground(e.CellBounds, true);

            // 2. Calculer la zone du rectangle
            Color currentBg = isClicked ? bgClick : (isHovered ? bgHover : bg);
            Rectangle btnRect = new Rectangle(e.CellBounds.Left + 4, e.CellBounds.Top + 4, e.CellBounds.Width - 8, e.CellBounds.Height - 8);

            // 3. Dessiner le rectangle de fond et sa bordure
            using (var brush = new SolidBrush(currentBg))
                e.Graphics.FillRectangle(brush, btnRect);

            using (var pen = new Pen(borderColor))
                e.Graphics.DrawRectangle(pen, btnRect);

            // 4. Charger et dessiner l'icône depuis le dossier image/ ou la racine
            string path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "image", iconFilename);
            if (!System.IO.File.Exists(path)) path = System.IO.Path.Combine("image", iconFilename);
            if (!System.IO.File.Exists(path)) path = System.IO.Path.Combine(Application.StartupPath, iconFilename);

            if (System.IO.File.Exists(path))
            {
                using (Image img = Image.FromFile(path))
                {
                    int x = btnRect.Left + (btnRect.Width - iconSize) / 2;
                    int y = btnRect.Top + (btnRect.Height - iconSize) / 2;
                    e.Graphics.DrawImage(img, new Rectangle(x, y, iconSize, iconSize));
                }
            }

            // 5. Annuler le rendu par défaut de WinForms
            e.Handled = true;
        }
        private static GraphicsPath GetRoundedPath(Rectangle rect, int radius)
    {
        GraphicsPath path = new GraphicsPath();
        int diameter = radius * 2;
        path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
        path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
        path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }

        private void ChargerEmployes()
        {
            DataTable dtSource = DatabaseHelper.ExecuteQuery(@"
        SELECT id, (nom || ' ' || prenom) AS affichage 
        FROM Employe 
        WHERE statut = 'Actif' 
        ORDER BY nom");

            // Créer une structure propre avec les bons types C#
            DataTable t = new DataTable();
            t.Columns.Add("id", typeof(object)); // Accepte long et DBNull
            t.Columns.Add("affichage", typeof(string)); // Force le type string

            // Ajouter la ligne d'invite
            t.Rows.Add(DBNull.Value, "-- choisir un employé --");

            // Copier les données chargées
            foreach (DataRow row in dtSource.Rows)
            {
                t.Rows.Add(row["id"], row["affichage"]?.ToString());
            }

            cmbEmploye.DataSource = null;
            cmbEmploye.DisplayMember = "affichage";
            cmbEmploye.ValueMember = "id";
            cmbEmploye.DataSource = t;
            cmbEmploye.SelectedIndex = 0;
        }
        private void ChargerMouvementExistant(long id)
        {
            var dtMvt = DatabaseHelper.ExecuteQuery(@"
                SELECT nom, reference, type_mouvement, employe_id, date_mouvement, contenu, observation 
                FROM Mouvement WHERE id = @id", new SqliteParameter("@id", id));

            if (dtMvt.Rows.Count == 0) return;

            var row = dtMvt.Rows[0];
            if (row["nom"] != DBNull.Value) cmbNomMouvement.SelectedItem = row["nom"].ToString();
            if (row["type_mouvement"] != DBNull.Value) cmbTypeMouvement.SelectedItem = row["type_mouvement"].ToString();
            if (row["reference"] != DBNull.Value) txtReference.Text = row["reference"].ToString();
            if (row["observation"] != DBNull.Value) txtObservationGenerale.Text = row["observation"].ToString();
            if (row["contenu"] != DBNull.Value) txtContenu.Text = row["contenu"].ToString();
            if (row["employe_id"] != DBNull.Value)
            {
                long empId = Convert.ToInt64(row["employe_id"]);

                // Forcer la sélection en recherchant directement l'élément par sa valeur
                cmbEmploye.SelectedValue = empId;

                // Sécurité supplémentaire si Guna2ComboBox n'a pas mis à jour l'index
                if (cmbEmploye.SelectedIndex == -1 || cmbEmploye.SelectedIndex == 0)
                {
                    foreach (DataRowView item in cmbEmploye.Items)
                    {
                        if (item["id"] != DBNull.Value && Convert.ToInt64(item["id"]) == empId)
                        {
                            cmbEmploye.SelectedItem = item;
                            break;
                        }
                    }
                }
            }
            if (row["date_mouvement"] != DBNull.Value && DateTime.TryParse(row["date_mouvement"].ToString(), out DateTime dt))
                dtpDateMouvement.Value = dt;

            // Charger les lignes associées
            var dtLignes = DatabaseHelper.ExecuteQuery(@"
                SELECT 
                    lm.equipement_id AS equipement_id, 
                    m.designation AS designation, 
                    e.numero_serie AS num_serie, 
                    lm.etat_a_la_mouvement, 
                    lm.est_sortie,
                    lm.observation AS observation
                FROM Ligne_mouvement lm
                JOIN Equipement e ON lm.equipement_id = e.id
                JOIN Modele m ON e.modele_id = m.id
                WHERE lm.mouvement_id = @id", new SqliteParameter("@id", id));

            _lignes.Clear();
            foreach (DataRow r in dtLignes.Rows)
            {
                _lignes.Add(new LigneMouvementTemp
                {
                    // ✅ Utiliser le nom exact de la colonne sélectionnée dans le SELECT SQL
                    EquipementId = Convert.ToInt32(r["equipement_id"]),
                    Affichage = $"{r["designation"]} (S/N: {r["num_serie"]})",
                    Etat = r["etat_a_la_mouvement"]?.ToString() ?? "Bon",
                    EstSortie = Convert.ToInt32(r["est_sortie"]) == 1,
                    Observation = r["observation"]?.ToString() ?? string.Empty
                });
            }
        }

        private void RafraichirGrille()
        {
            dgvLignes.DataSource = null;
            dgvLignes.DataSource = _lignes;
        }

        private void BtnNouvelEmploye_Click(object? sender, EventArgs e)
        {
            using (var frm = new FrmAjouterEmploye())
            {
                if (frm.ShowDialog(this) == DialogResult.OK && frm.EmployeIdResultat.HasValue)
                {
                    ChargerEmployes();
                    cmbEmploye.SelectedValue = Convert.ToInt64(frm.EmployeIdResultat.Value);
                }
            }
        }

        private void BtnAjouterLigne_Click(object? sender, EventArgs e)
        {
            using (var frm = new FrmAjouterLigneMouvement())
            {
                if (frm.ShowDialog(this) == DialogResult.OK && frm.LigneResultat != null)
                {
                    _lignes.Add(frm.LigneResultat);
                    RafraichirGrille();
                }
            }
        }

        private void DgvLignes_CellMouseClick(object? sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _lignes.Count) return;
            string colName = dgvLignes.Columns[e.ColumnIndex].Name;

            if (colName == "colSupprimerLigne")
            {
                _lignes.RemoveAt(e.RowIndex);
                RafraichirGrille();
            }
            else if (colName == "colModifierLigne")
            {
                var ligneActuelle = _lignes[e.RowIndex];
                using (var frm = new FrmAjouterLigneMouvement(ligneActuelle))
                {
                    if (frm.ShowDialog(this) == DialogResult.OK && frm.LigneResultat != null)
                    {
                        _lignes[e.RowIndex] = frm.LigneResultat;
                        RafraichirGrille();
                    }
                }
            }
        }

        private static string DeterminerNouveauStatut(string typeMouvement, bool estSortie)
        {
            if (!estSortie) return "En stock";

            return typeMouvement switch
            {
                "Affectation" => "Affecté",
                "Prêt" => "En prêt",
                "Maintenance" => "En réparation",
                "Réforme" => "Réformé",
                _ => "En stock"
            };
        }

        private void BtnEnregistrer_Click(object? sender, EventArgs e)
        {
            if (cmbNomMouvement.SelectedItem == null || cmbTypeMouvement.SelectedItem == null)
            {
                MessageBox.Show("Le type de document et le type de mouvement sont obligatoires.", "Champs manquants",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (_lignes.Count == 0)
            {
                MessageBox.Show("Ajoutez au moins une ligne avant d'enregistrer.", "Aucune ligne",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (cmbEmploye.SelectedIndex <= 0 || cmbEmploye.SelectedValue == DBNull.Value || cmbEmploye.SelectedValue == null)
            {
                MessageBox.Show("Veuillez choisir un employé valide dans la liste.", "Employé obligatoire",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cmbEmploye.Focus();
                return;
            }

            string typeMouvement = cmbTypeMouvement.SelectedItem.ToString()!;
            string nomMouvement = cmbNomMouvement.SelectedItem.ToString()!;

            using (var conn = DatabaseHelper.GetConnection())
            {
                using (var tx = conn.BeginTransaction())
                {
                    try
                    {
                        long mouvementId;

                        if (_mouvementIdToEdit.HasValue)
                        {
                            mouvementId = _mouvementIdToEdit.Value;

                            // 1. UPDATE du Mouvement
                            using (var cmd = conn.CreateCommand())
                            {
                                cmd.Transaction = tx;
                                cmd.CommandText = @"
                                    UPDATE Mouvement 
                                    SET nom = @nom, reference = @ref, type_mouvement = @type, 
                                        employe_id = @emp, date_mouvement = @date, 
                                        contenu = @contenu, observation = @obs
                                    WHERE id = @id;";
                                cmd.Parameters.AddWithValue("@id", mouvementId);
                                cmd.Parameters.AddWithValue("@nom", nomMouvement);
                                cmd.Parameters.AddWithValue("@ref", string.IsNullOrWhiteSpace(txtReference.Text) ? (object)DBNull.Value : txtReference.Text.Trim());
                                cmd.Parameters.AddWithValue("@type", typeMouvement);
                                cmd.Parameters.AddWithValue("@emp", cmbEmploye.SelectedValue ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@date", dtpDateMouvement.Value.ToString("yyyy-MM-dd"));
                                cmd.Parameters.AddWithValue("@contenu", string.IsNullOrWhiteSpace(txtContenu.Text) ? (object)DBNull.Value : txtContenu.Text.Trim());
                                cmd.Parameters.AddWithValue("@obs", string.IsNullOrWhiteSpace(txtObservationGenerale.Text) ? (object)DBNull.Value : txtObservationGenerale.Text.Trim());
                                cmd.ExecuteNonQuery();
                            }

                            // 2. Supprimer les anciennes lignes pour réinsérer les nouvelles
                            using (var cmd = conn.CreateCommand())
                            {
                                cmd.Transaction = tx;
                                cmd.CommandText = "DELETE FROM Ligne_mouvement WHERE mouvement_id = @mvt;";
                                cmd.Parameters.AddWithValue("@mvt", mouvementId);
                                cmd.ExecuteNonQuery();
                            }
                        }
                        else
                        {
                            // INSERT du Mouvement
                            string codeMouvement = $"MVT-{DateTime.Now:yyyyMMddHHmmssfff}";
                            using (var cmd = conn.CreateCommand())
                            {
                                cmd.Transaction = tx;
                                cmd.CommandText = @"
                                    INSERT INTO Mouvement (code_mouvement, nom, reference, type_mouvement, employe_id, date_mouvement, contenu, observation)
                                    VALUES (@code, @nom, @ref, @type, @emp, @date, @contenu, @obs);
                                    SELECT last_insert_rowid();";
                                cmd.Parameters.AddWithValue("@code", codeMouvement);
                                cmd.Parameters.AddWithValue("@nom", nomMouvement);
                                cmd.Parameters.AddWithValue("@ref", string.IsNullOrWhiteSpace(txtReference.Text) ? (object)DBNull.Value : txtReference.Text.Trim());
                                cmd.Parameters.AddWithValue("@type", typeMouvement);
                                cmd.Parameters.AddWithValue("@emp", cmbEmploye.SelectedValue ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@date", dtpDateMouvement.Value.ToString("yyyy-MM-dd"));
                                cmd.Parameters.AddWithValue("@contenu", string.IsNullOrWhiteSpace(txtContenu.Text) ? (object)DBNull.Value : txtContenu.Text.Trim());
                                cmd.Parameters.AddWithValue("@obs", string.IsNullOrWhiteSpace(txtObservationGenerale.Text) ? (object)DBNull.Value : txtObservationGenerale.Text.Trim());

                                mouvementId = (long)cmd.ExecuteScalar()!;
                            }
                        }

                        // Réinsertion/Insertion des lignes + mise à jour des équipements
                        foreach (var ligne in _lignes)
                        {
                            using (var cmd = conn.CreateCommand())
                            {
                                cmd.Transaction = tx;
                                cmd.CommandText = @"
                                    INSERT INTO Ligne_mouvement (mouvement_id, equipement_id, etat_a_la_mouvement, est_sortie, observation)
                                    VALUES (@mvt, @eq, @etat, @sortie, @obs);";
                                cmd.Parameters.AddWithValue("@mvt", mouvementId);
                                cmd.Parameters.AddWithValue("@eq", ligne.EquipementId);
                                cmd.Parameters.AddWithValue("@etat", ligne.Etat);
                                cmd.Parameters.AddWithValue("@sortie", ligne.EstSortie ? 1 : 0);
                                cmd.Parameters.AddWithValue("@obs", string.IsNullOrWhiteSpace(ligne.Observation) ? (object)DBNull.Value : ligne.Observation.Trim());
                                cmd.ExecuteNonQuery();
                            }

                            string nouveauStatut = DeterminerNouveauStatut(typeMouvement, ligne.EstSortie);
                            using (var cmd = conn.CreateCommand())
                            {
                                cmd.Transaction = tx;
                                cmd.CommandText = @"
                                    UPDATE Equipement 
                                    SET statut = @statut, date_modification = CURRENT_TIMESTAMP
                                    WHERE id = @id;";
                                cmd.Parameters.AddWithValue("@statut", nouveauStatut);
                                cmd.Parameters.AddWithValue("@id", ligne.EquipementId);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        tx.Commit();
                    }
                    catch (SqliteException ex)
                    {
                        tx.Rollback();
                        MessageBox.Show(
                            "Enregistrement annulé : aucune donnée n'a été modifiée.\n\n" +
                            "Cause probable : référence invalide (employé ou équipement supprimé entre-temps).\n\n" +
                            "Détail : " + ex.Message,
                            "Erreur base de données", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    catch (Exception ex)
                    {
                        tx.Rollback();
                        MessageBox.Show(
                            "Enregistrement annulé : aucune donnée n'a été modifiée.\n\nDétail : " + ex.Message,
                            "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }
            }

            MouvementEnregistre = true;
            _mainForm?.ChargerEquipements();
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}