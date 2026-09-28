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
using System.Linq;
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
        private Guna2TextBox txtAQui = null!;

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

            MakeLabel("À qui *", margeG);
            txtAQui = new Guna2TextBox
            {
                Left = margeG,
                Top = y + 20,
                Width = largeurChamp,
                Height = 36,
                BorderRadius = 6
            };
            Controls.Add(txtAQui);

            MakeLabel("Contenu du document (Texte d'attestation)", margeD);
            txtContenu = new Guna2TextBox
            {
                Left = margeD,
                Top = y + 20,
                Width = largeurChamp,
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

            dgvLignes.Columns.Add(new DataGridViewTextBoxColumn { Name = "colAffichage", HeaderText = "Modèle", DataPropertyName = "Affichage", Width = 210 });
            dgvLignes.Columns.Add(new DataGridViewTextBoxColumn { Name = "colQuantite", HeaderText = "Quantité", DataPropertyName = "Quantite", Width = 65 });
            dgvLignes.Columns.Add(new DataGridViewTextBoxColumn { Name = "colEtat", HeaderText = "État", DataPropertyName = "Etat", Width = 80 });
            dgvLignes.Columns.Add(new DataGridViewCheckBoxColumn { Name = "colSortie", HeaderText = "Sortie ?", DataPropertyName = "EstSortie", Width = 60 });
            dgvLignes.Columns.Add(new DataGridViewTextBoxColumn { Name = "colObs", HeaderText = "Observation", DataPropertyName = "Observation", Width = 120 });
            dgvLignes.Columns.Add(new DataGridViewTextBoxColumn { Name = "colModifierLigne", HeaderText = "Modifier", Width = 70, ReadOnly = true });
            dgvLignes.Columns.Add(new DataGridViewTextBoxColumn { Name = "colSupprimerLigne", HeaderText = "Supprimer", Width = 75, ReadOnly = true });

            dgvLignes.CellMouseClick += DgvLignes_CellMouseClick;
            dgvLignes.CellPainting += DgvLignes_CellPainting;

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

            e.PaintBackground(e.CellBounds, true);

            Color currentBg = isClicked ? bgClick : (isHovered ? bgHover : bg);
            Rectangle btnRect = new Rectangle(e.CellBounds.Left + 4, e.CellBounds.Top + 4, e.CellBounds.Width - 8, e.CellBounds.Height - 8);

            using (var brush = new SolidBrush(currentBg))
                e.Graphics.FillRectangle(brush, btnRect);

            using (var pen = new Pen(borderColor))
                e.Graphics.DrawRectangle(pen, btnRect);

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

            e.Handled = true;
        }

        private void ChargerEmployes()
        {
            DataTable dtSource = DatabaseHelper.ExecuteQuery(@"
                SELECT id, (nom || ' ' || prenom) AS affichage 
                FROM Employe 
                WHERE statut = 'Actif' 
                ORDER BY nom");

            DataTable t = new DataTable();
            t.Columns.Add("id", typeof(object));
            t.Columns.Add("affichage", typeof(string));

            t.Rows.Add(DBNull.Value, "-- choisir un employé --");

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
                SELECT nom, reference, type_mouvement, employe_id, date_mouvement, contenu, observation, a_qui 
                FROM Mouvement WHERE id = @id", new SqliteParameter("@id", id));

            if (dtMvt.Rows.Count == 0) return;

            var row = dtMvt.Rows[0];
            if (row["nom"] != DBNull.Value) cmbNomMouvement.SelectedItem = row["nom"].ToString();
            if (row["type_mouvement"] != DBNull.Value) cmbTypeMouvement.SelectedItem = row["type_mouvement"].ToString();
            if (row["reference"] != DBNull.Value) txtReference.Text = row["reference"].ToString();
            if (row["observation"] != DBNull.Value) txtObservationGenerale.Text = row["observation"].ToString();
            if (row["a_qui"] != DBNull.Value) txtAQui.Text = row["a_qui"].ToString();
            if (row["contenu"] != DBNull.Value) txtContenu.Text = row["contenu"].ToString();
            if (row["employe_id"] != DBNull.Value)
            {
                long empId = Convert.ToInt64(row["employe_id"]);
                cmbEmploye.SelectedValue = empId;

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

            var dtLignes = DatabaseHelper.ExecuteQuery(@"
                SELECT 
                    lm.modele_id AS modele_id,
                    TRIM(COALESCE(c.designation,'') || ' ' || COALESCE(mq.designation,'') || ' ' || m.designation || ' ' || COALESCE(m.reference,'')) AS designation,
                    lm.quantite AS quantite,
                    lm.etat_a_la_mouvement, 
                    lm.est_sortie,
                    lm.observation AS observation
                FROM Ligne_mouvement lm
                JOIN Modele m ON lm.modele_id = m.id
                LEFT JOIN Categorie c ON m.categorie_id = c.id
                LEFT JOIN Marque mq ON m.marque_id = mq.id
                WHERE lm.mouvement_id = @id", new SqliteParameter("@id", id));

            _lignes.Clear();
            foreach (DataRow r in dtLignes.Rows)
            {
                int quantite = Convert.ToInt32(r["quantite"]);
                _lignes.Add(new LigneMouvementTemp
                {
                    ModeleId = Convert.ToInt32(r["modele_id"]),
                    Affichage = $"{r["designation"]}  x {quantite}",
                    Quantite = quantite,
                    Etat = r["etat_a_la_mouvement"]?.ToString() ?? "Bon",
                    // NULL traité comme une entrée, exactement comme le trigger trg_mvt_stock_insert
                    EstSortie = r["est_sortie"] != DBNull.Value && Convert.ToInt32(r["est_sortie"]) == 1,
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
            using (var frm = new FrmAjouterLigneMouvement(null, _lignes.ToList(), _mouvementIdToEdit))
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
                var autresLignes = _lignes.Where((l, idx) => idx != e.RowIndex).ToList();
                using (var frm = new FrmAjouterLigneMouvement(ligneActuelle, autresLignes, _mouvementIdToEdit))
                {
                    if (frm.ShowDialog(this) == DialogResult.OK && frm.LigneResultat != null)
                    {
                        _lignes[e.RowIndex] = frm.LigneResultat;
                        RafraichirGrille();
                    }
                }
            }
        }

        private void BtnEnregistrer_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtAQui.Text))
            {
                MessageBox.Show("Le champ 'À qui' est obligatoire.", "Champ manquant",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtAQui.Focus();
                return;
            }
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

            // -----------------------------------------------------------------------------------------
            // 1. VALIDATION DE COHÉRENCE : Type de Mouvement vs Lignes (Sortie / Retour)
            // -----------------------------------------------------------------------------------------
            bool estUnMouvementDeRetour = typeMouvement.Equals("Retour", StringComparison.OrdinalIgnoreCase);

            if (estUnMouvementDeRetour)
            {
                // Si le type est "Retour", il faut qu'au moins une ligne soit un Retour (est_sortie = false)
                bool contientUnRetour = _lignes.Any(l => !l.EstSortie);
                if (!contientUnRetour)
                {
                    MessageBox.Show(
                        "Pour un type de mouvement 'Retour', la liste doit contenir au moins une ligne de type Retour (Sortie décochée).",
                        "Incohérence des lignes",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }
            }
            else
            {
                // Pour les types "Affectation", "Prêt", "Maintenance", "Réforme", il faut au moins une ligne de type Sortie (est_sortie = true)
                bool contientUneSortie = _lignes.Any(l => l.EstSortie);
                if (!contientUneSortie)
                {
                    MessageBox.Show(
                        $"Pour un type de mouvement '{typeMouvement}', la liste doit contenir au moins une ligne de type Sortie (Sortie cochée).",
                        "Incohérence des lignes",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }
            }

            if (_lignes.Any(l => l.Quantite <= 0))
            {
                MessageBox.Show("Toutes les lignes doivent avoir une quantité supérieure à 0.", "Quantité invalide",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Anti double-clic : évite deux enregistrements successifs du même mouvement
            btnEnregistrer.Enabled = false;
            (string? Message, string Titre) resultat;
            try
            {
                resultat = TenterEnregistrer(nomMouvement, typeMouvement);
            }
            finally
            {
                btnEnregistrer.Enabled = true;
            }

            // Les MessageBox sont affichées APRÈS la fermeture de la transaction (aucun verrou SQLite maintenu
            // pendant que l'utilisateur lit le message).
            if (resultat.Message != null)
            {
                MessageBox.Show(resultat.Message, resultat.Titre, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            MouvementEnregistre = true;
            DialogResult = DialogResult.OK;
            Close();
        }

        /// <summary>
        /// Enregistre le mouvement et ses lignes dans UNE transaction, avec un contrôle de stock préalable.
        ///
        /// IMPORTANT : Modele.quantite est mise à jour par les TRIGGERS de la base
        /// (trg_mvt_stock_insert / trg_mvt_stock_delete sur Ligne_mouvement). Ce code ne doit donc JAMAIS
        /// faire UPDATE Modele SET quantite = ... (sinon double comptage).
        ///
        /// Déroulement (nombre minimal d'accès base) :
        ///   1. Σ entrées et Σ sorties par modèle sur les nouvelles lignes      (mémoire)
        ///   2. Édition : lecture des anciennes lignes du mouvement             (1 requête)
        ///   3. Lecture du stock de tous les modèles concernés                  (1 requête)
        ///   4. Stock final = stock - effet des anciennes lignes + effet des nouvelles ; refus si &lt; 0
        ///   5. Seulement si tout est valide : écriture (mouvement + lignes)
        /// Retourne (null, "") si succès, sinon (message d'erreur, titre) sans qu'aucune donnée n'ait été modifiée.
        /// </summary>
        private (string? Message, string Titre) TenterEnregistrer(string nomMouvement, string typeMouvement)
        {
            // ---- 1. Somme des entrées / sorties par modèle (nouvelles lignes) ----
            var nouveau = new Dictionary<int, (long Entrees, long Sorties)>();
            foreach (var l in _lignes)
            {
                nouveau.TryGetValue(l.ModeleId, out var cumul);
                nouveau[l.ModeleId] = l.EstSortie
                    ? (cumul.Entrees, cumul.Sorties + l.Quantite)
                    : (cumul.Entrees + l.Quantite, cumul.Sorties);
            }

            try
            {
                using (var conn = OuvrirConnexion())
                using (var tx = conn.BeginTransaction())
                {
                    // ---- 2. Anciennes lignes du mouvement (mode édition) ----
                    var anciennes = new Dictionary<int, (long Entrees, long Sorties)>();
                    long dernierIdAncien = 0;

                    if (_mouvementIdToEdit.HasValue)
                    {
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.Transaction = tx;
                            cmd.CommandText = "SELECT id, modele_id, quantite, est_sortie FROM Ligne_mouvement WHERE mouvement_id = @mvt;";
                            cmd.Parameters.AddWithValue("@mvt", _mouvementIdToEdit.Value);
                            using (var rd = cmd.ExecuteReader())
                            {
                                while (rd.Read())
                                {
                                    long idLigne = rd.GetInt64(0);
                                    int mdl = (int)rd.GetInt64(1);
                                    long qte = rd.GetInt64(2);
                                    // NULL = entrée, comme dans le trigger (CASE WHEN est_sortie = 1 ... ELSE ...)
                                    bool sortie = !rd.IsDBNull(3) && rd.GetInt64(3) == 1;

                                    if (idLigne > dernierIdAncien) dernierIdAncien = idLigne;
                                    anciennes.TryGetValue(mdl, out var cumulAnc);
                                    anciennes[mdl] = sortie
                                        ? (cumulAnc.Entrees, cumulAnc.Sorties + qte)
                                        : (cumulAnc.Entrees + qte, cumulAnc.Sorties);
                                }
                            }
                        }
                    }

                    // ---- 3. Stock actuel de tous les modèles concernés (1 seule requête) ----
                    var idsConcernes = nouveau.Keys.Union(anciennes.Keys).ToList();
                    var stocks = new Dictionary<int, (long Quantite, string Designation)>();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.Transaction = tx;
                        var noms = new List<string>();
                        for (int i = 0; i < idsConcernes.Count; i++)
                        {
                            noms.Add("@p" + i);
                            cmd.Parameters.AddWithValue("@p" + i, idsConcernes[i]);
                        }
                        cmd.CommandText = $"SELECT id, quantite, designation FROM Modele WHERE id IN ({string.Join(",", noms)});";
                        using (var rd = cmd.ExecuteReader())
                        {
                            while (rd.Read())
                                stocks[(int)rd.GetInt64(0)] = (rd.GetInt64(1), rd.IsDBNull(2) ? "" : rd.GetString(2));
                        }
                    }

                    // ---- 4. Vérification : stock final >= 0 pour CHAQUE modèle ----
                    var problemes = new List<string>();
                    foreach (int id in idsConcernes)
                    {
                        if (!stocks.TryGetValue(id, out var stock))
                            return ("Un des modèles de la liste n'existe plus dans la base.\n\nEnregistrement annulé, aucune donnée n'a été modifiée.", "Modèle introuvable");

                        anciennes.TryGetValue(id, out var anc);
                        nouveau.TryGetValue(id, out var nou);

                        // Stock tel qu'il serait SANS ce mouvement (les anciennes lignes sont déjà comptées en base)
                        long stockSansMouvement = stock.Quantite + anc.Sorties - anc.Entrees;
                        long stockFinal = stockSansMouvement + nou.Entrees - nou.Sorties;

                        if (stockFinal < 0)
                        {
                            problemes.Add($"• {stock.Designation} : stock disponible {stockSansMouvement}, " +
                                          $"entrées {nou.Entrees}, sorties demandées {nou.Sorties}  →  manque {-stockFinal}");
                        }
                    }

                    if (problemes.Count > 0)
                    {
                        return ("Stock insuffisant pour :\n\n" + string.Join("\n", problemes) +
                                "\n\nEnregistrement annulé, aucune donnée n'a été modifiée.",
                                "Quantité incompatible avec le stock");
                    }

                    // ---- 5. Écriture (aucune erreur métier possible à partir d'ici) ----
                    long mouvementId;

                    if (_mouvementIdToEdit.HasValue)
                    {
                        mouvementId = _mouvementIdToEdit.Value;
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.Transaction = tx;
                            cmd.CommandText = @"
                                UPDATE Mouvement 
                                SET nom = @nom, reference = @ref, type_mouvement = @type, 
                                    employe_id = @emp, date_mouvement = @date, 
                                    contenu = @contenu, observation = @obs, a_qui = @a_qui
                                WHERE id = @id;";
                            AjouterParametresMouvement(cmd, nomMouvement, typeMouvement);
                            cmd.Parameters.AddWithValue("@id", mouvementId);
                            if (cmd.ExecuteNonQuery() == 0)
                                return ("Ce mouvement n'existe plus dans la base (supprimé entre-temps).\n\nEnregistrement annulé.", "Mouvement introuvable");
                        }
                    }
                    else
                    {
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.Transaction = tx;
                            cmd.CommandText = @"
                                INSERT INTO Mouvement (code_mouvement, nom, reference, type_mouvement, employe_id, date_mouvement, contenu, observation, a_qui)
                                VALUES (@code, @nom, @ref, @type, @emp, @date, @contenu, @obs, @a_qui);
                                SELECT last_insert_rowid();";
                            cmd.Parameters.AddWithValue("@code", $"MVT-{DateTime.Now:yyyyMMddHHmmssfff}");
                            AjouterParametresMouvement(cmd, nomMouvement, typeMouvement);
                            mouvementId = Convert.ToInt64(cmd.ExecuteScalar());
                        }
                    }

                    // Insertion d'un lot de lignes avec UNE commande réutilisée. Les triggers de la base
                    // mettent à jour Modele.quantite (et date_modification) ; le code C# n'y touche pas.
                    void InsererLignes(IEnumerable<LigneMouvementTemp> lignes)
                    {
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.Transaction = tx;
                            cmd.CommandText = @"
                                INSERT INTO Ligne_mouvement (mouvement_id, modele_id, quantite, etat_a_la_mouvement, est_sortie, observation)
                                VALUES (@mvt, @mdl, @qte, @etat, @sortie, @obs);";
                            var pMvt = cmd.Parameters.Add("@mvt", SqliteType.Integer);
                            var pMdl = cmd.Parameters.Add("@mdl", SqliteType.Integer);
                            var pQte = cmd.Parameters.Add("@qte", SqliteType.Integer);
                            var pEtat = cmd.Parameters.Add("@etat", SqliteType.Text);
                            var pSortie = cmd.Parameters.Add("@sortie", SqliteType.Integer);
                            var pObs = cmd.Parameters.Add("@obs", SqliteType.Text);
                            pMvt.Value = mouvementId;

                            foreach (var l in lignes)
                            {
                                pMdl.Value = l.ModeleId;
                                pQte.Value = l.Quantite;
                                pEtat.Value = l.Etat;
                                pSortie.Value = l.EstSortie ? 1 : 0;
                                pObs.Value = string.IsNullOrWhiteSpace(l.Observation) ? DBNull.Value : l.Observation.Trim();
                                cmd.ExecuteNonQuery();
                            }
                        }
                    }

                    // ORDRE VOLONTAIRE : le CHECK (quantite >= 0) de Modele est évalué après CHAQUE ligne par les
                    // triggers. Le stock final étant validé ci-dessus, on ordonne les opérations pour qu'il ne
                    // descende jamais sous sa valeur finale en cours de route :
                    //   (a) nouvelles ENTRÉES d'abord (le stock ne fait que monter),
                    //   (b) suppression des anciennes lignes (id <= dernierIdAncien ; les triggers restituent le stock),
                    //   (c) nouvelles SORTIES en dernier (le stock ne fait que descendre jusqu'à sa valeur finale).
                    InsererLignes(_lignes.Where(l => !l.EstSortie));

                    if (dernierIdAncien > 0)
                    {
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.Transaction = tx;
                            cmd.CommandText = "DELETE FROM Ligne_mouvement WHERE mouvement_id = @mvt AND id <= @maxId;";
                            cmd.Parameters.AddWithValue("@mvt", mouvementId);
                            cmd.Parameters.AddWithValue("@maxId", dernierIdAncien);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    InsererLignes(_lignes.Where(l => l.EstSortie));

                    tx.Commit();
                    return (null, "");
                }
                // Toute sortie sans Commit (return / exception) => la transaction est annulée à sa libération.
            }
            catch (SqliteException ex)
            {
                string message = ex.SqliteErrorCode == 19
                    ? "Enregistrement annulé : une contrainte de la base a été violée (stock négatif ou donnée invalide).\n" +
                      "Aucune donnée n'a été modifiée. Vérifiez les quantités demandées.\n\nDétail : " + ex.Message
                    : "Enregistrement annulé : aucune donnée n'a été modifiée.\n\n" +
                      "Cause probable : base verrouillée ou référence invalide (employé ou modèle supprimé entre-temps).\n\n" +
                      "Détail : " + ex.Message;
                return (message, "Erreur base de données");
            }
            catch (Exception ex)
            {
                return ("Enregistrement annulé : aucune donnée n'a été modifiée.\n\nDétail : " + ex.Message, "Erreur");
            }
        }

        // Certaines versions de DatabaseHelper.GetConnection() renvoient une connexion déjà ouverte,
        // d'autres non (Form1 appelle conn.Open() après GetConnection) : on couvre les deux cas.
        private static SqliteConnection OuvrirConnexion()
        {
            var conn = DatabaseHelper.GetConnection();
            if (conn.State != ConnectionState.Open)
                conn.Open();
            return conn;
        }

        private void AjouterParametresMouvement(SqliteCommand cmd, string nomMouvement, string typeMouvement)
        {
            cmd.Parameters.AddWithValue("@nom", nomMouvement);
            cmd.Parameters.AddWithValue("@ref", string.IsNullOrWhiteSpace(txtReference.Text) ? (object)DBNull.Value : txtReference.Text.Trim());
            cmd.Parameters.AddWithValue("@type", typeMouvement);
            cmd.Parameters.AddWithValue("@emp", cmbEmploye.SelectedValue ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@date", dtpDateMouvement.Value.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("@contenu", string.IsNullOrWhiteSpace(txtContenu.Text) ? (object)DBNull.Value : txtContenu.Text.Trim());
            cmd.Parameters.AddWithValue("@obs", string.IsNullOrWhiteSpace(txtObservationGenerale.Text) ? (object)DBNull.Value : txtObservationGenerale.Text.Trim());
            cmd.Parameters.AddWithValue("@a_qui", txtAQui.Text.Trim());
        }
    }
}