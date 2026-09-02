#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using Guna.UI2.WinForms;
using InventoryApp.Data;
using Microsoft.Data.Sqlite;

namespace InventoryApp
{
    public class FrmAjouterInventaire : Form
    {
        // Palette de couleurs personnalisée identique aux formulaires de mouvement
        private readonly Color _primaryBlue = Color.FromArgb(37, 99, 235);
        private readonly Color _darkNavy = Color.FromArgb(24, 30, 54);
        private readonly Color _lightGray = Color.FromArgb(240, 242, 245);

        public bool InventaireEnregistre { get; private set; } = false;

        private readonly Form1? _mainForm;
        private readonly int? _inventaireIdEnEdition;
        private bool EnModeEdition => _inventaireIdEnEdition.HasValue;

        private readonly BindingList<LigneInventaireTemp> _lignes = new BindingList<LigneInventaireTemp>();

        private Guna2TextBox txtStructure = null!;
        private Guna2TextBox txtBureau = null!;
        private Guna2DateTimePicker dtpDateInventaire = null!;

        private Guna2DataGridView dgvLignes = null!;
        private Guna2Button btnAjouterLigne = null!;
        private Guna2Button btnEnregistrer = null!;
        private Guna2Button btnAnnuler = null!;
        private Label lblTitre = null!;

        public FrmAjouterInventaire(Form1? mainForm, int? inventaireIdEnEdition = null)
        {
            _mainForm = mainForm;
            _inventaireIdEnEdition = inventaireIdEnEdition;

            Text = EnModeEdition ? "Modifier la fiche d'inventaire" : "Nouvelle fiche d'inventaire";
            Size = new Size(820, 620);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.None;
            BackColor = Color.White;

            ConstruireControles();
            Load += (s, e) => { if (EnModeEdition) ChargerDonneesExistantes(); };
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
                Text = EnModeEdition ? "MODIFIER LA FICHE D'INVENTAIRE" : "NOUVELLE FICHE D'INVENTAIRE",
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

            MakeLabel("Structure *", margeG);
            txtStructure = new Guna2TextBox
            {
                Left = margeG,
                Top = y + 20,
                Width = largeurChamp,
                Height = 36,
                BorderRadius = 6,
                PlaceholderText = "Ex: SERVICE MAINTENANCE"
            };
            Controls.Add(txtStructure);

            MakeLabel("Bureau *", margeD);
            txtBureau = new Guna2TextBox
            {
                Left = margeD,
                Top = y + 20,
                Width = largeurChamp,
                Height = 36,
                BorderRadius = 6,
                PlaceholderText = "Ex: RESEAUX"
            };
            Controls.Add(txtBureau);

            y += 65;

            MakeLabel("Date de l'inventaire", margeG);
            dtpDateInventaire = new Guna2DateTimePicker
            {
                Left = margeG,
                Top = y + 20,
                Width = largeurChamp,
                Height = 36,
                BorderRadius = 6,
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Today,
                FillColor = _lightGray,
                ForeColor = _darkNavy
            };
            Controls.Add(dtpDateInventaire);

            y += 75;

            var lblLignes = new Label
            {
                Text = "LIGNES D'INVENTAIRE",
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
                Height = 260,
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

            dgvLignes.Columns.Add(new DataGridViewTextBoxColumn { Name = "colModele", HeaderText = "Modèle", DataPropertyName = "AffichageModele", Width = 220 });
            dgvLignes.Columns.Add(new DataGridViewTextBoxColumn { Name = "colEquip", HeaderText = "Équipement", DataPropertyName = "AffichageEquipement", Width = 230 });
            dgvLignes.Columns.Add(new DataGridViewTextBoxColumn { Name = "colQte", HeaderText = "Qté", DataPropertyName = "Quantite", Width = 50 });
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

            y += 275;

            btnEnregistrer = new Guna2Button
            {
                Text = EnModeEdition ? "Mettre à jour" : "Enregistrer l'inventaire",
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

        private void RafraichirGrille()
        {
            dgvLignes.DataSource = null;
            dgvLignes.DataSource = _lignes;
        }

        private void ChargerDonneesExistantes()
        {
            var tHead = DatabaseHelper.ExecuteQuery(
                "SELECT structure, bureau, date_inventaire FROM Inventaire WHERE id = @id",
                new SqliteParameter("@id", _inventaireIdEnEdition!.Value));

            if (tHead.Rows.Count == 0)
            {
                MessageBox.Show("Fiche d'inventaire introuvable.", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close();
                return;
            }

            var row = tHead.Rows[0];
            txtStructure.Text = row["structure"]?.ToString() ?? "";
            txtBureau.Text = row["bureau"]?.ToString() ?? "";
            if (DateTime.TryParse(row["date_inventaire"].ToString(), out var d))
                dtpDateInventaire.Value = d;

            // Requête nettoyée pour éviter toute syntaxe SQL invalide
            string sqlLignes = @"
                SELECT 
                    li.equipement_id, 
                    li.quantite, 
                    li.observation,
                    (COALESCE(c.designation, '') || ' ' || COALESCE(mq.designation, '') || ' ' || COALESCE(m.designation, '') || ' ' || COALESCE(m.reference, '')) AS affichage_modele,
                    (e.id || ' | ' || COALESCE(e.statut, '-') || ' | ' || COALESCE(e.etat, '-') || ' | ' || COALESCE(e.code_barre, '-') || ' | ' || COALESCE(e.numero_serie, '-')) AS affichage_eq
                FROM Ligne_inventaire li
                INNER JOIN Equipement e ON li.equipement_id = e.id
                INNER JOIN Modele m ON e.modele_id = m.id
                LEFT JOIN Marque mq ON m.marque_id = mq.id
                LEFT JOIN Categorie c ON m.categorie_id = c.id
                WHERE li.inventaire_id = @id";

            var tLignes = DatabaseHelper.ExecuteQuery(sqlLignes, new SqliteParameter("@id", _inventaireIdEnEdition!.Value));

            _lignes.Clear();
            foreach (DataRow r in tLignes.Rows)
            {
                _lignes.Add(new LigneInventaireTemp
                {
                    EquipementId = Convert.ToInt32(r["equipement_id"]),
                    AffichageModele = r["affichage_modele"]?.ToString()?.Trim() ?? "",
                    AffichageEquipement = r["affichage_eq"]?.ToString() ?? "",
                    Quantite = Convert.ToInt32(r["quantite"]),
                    Observation = r["observation"] == DBNull.Value ? null : r["observation"].ToString()
                });
            }
            RafraichirGrille();
        }
        private void BtnAjouterLigne_Click(object? sender, EventArgs e)
        {
            using (var frm = new FrmAjouterLigneInventaire())
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
                using (var frm = new FrmAjouterLigneInventaire(ligneActuelle))
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
            if (string.IsNullOrWhiteSpace(txtStructure.Text) || string.IsNullOrWhiteSpace(txtBureau.Text))
            {
                MessageBox.Show("Structure et Bureau sont obligatoires.", "Champs manquants",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (_lignes.Count == 0)
            {
                MessageBox.Show("Ajoutez au moins une ligne avant d'enregistrer.", "Aucune ligne",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (var conn = DatabaseHelper.GetConnection())
            {
                using (var tx = conn.BeginTransaction())
                {
                    try
                    {
                        long inventaireId;

                        if (EnModeEdition)
                        {
                            inventaireId = _inventaireIdEnEdition!.Value;

                            using (var cmd = conn.CreateCommand())
                            {
                                cmd.Transaction = tx;
                                cmd.CommandText = @"
                                    UPDATE Inventaire
                                    SET structure=@structure, bureau=@bureau, date_inventaire=@date
                                    WHERE id=@id;";
                                cmd.Parameters.AddWithValue("@structure", txtStructure.Text.Trim());
                                cmd.Parameters.AddWithValue("@bureau", txtBureau.Text.Trim());
                                cmd.Parameters.AddWithValue("@date", dtpDateInventaire.Value.ToString("yyyy-MM-dd"));
                                cmd.Parameters.AddWithValue("@id", inventaireId);
                                cmd.ExecuteNonQuery();
                            }

                            using (var cmd = conn.CreateCommand())
                            {
                                cmd.Transaction = tx;
                                cmd.CommandText = "DELETE FROM Ligne_inventaire WHERE inventaire_id = @id;";
                                cmd.Parameters.AddWithValue("@id", inventaireId);
                                cmd.ExecuteNonQuery();
                            }
                        }
                        else
                        {
                            using (var cmd = conn.CreateCommand())
                            {
                                cmd.Transaction = tx;
                                cmd.CommandText = @"
                                    INSERT INTO Inventaire (structure, bureau, date_inventaire)
                                    VALUES (@structure, @bureau, @date);
                                    SELECT last_insert_rowid();";
                                cmd.Parameters.AddWithValue("@structure", txtStructure.Text.Trim());
                                cmd.Parameters.AddWithValue("@bureau", txtBureau.Text.Trim());
                                cmd.Parameters.AddWithValue("@date", dtpDateInventaire.Value.ToString("yyyy-MM-dd"));
                                inventaireId = (long)cmd.ExecuteScalar()!;
                            }
                        }

                        foreach (var ligne in _lignes)
                        {
                            using (var cmd = conn.CreateCommand())
                            {
                                cmd.Transaction = tx;
                                cmd.CommandText = @"
                                    INSERT INTO Ligne_inventaire (inventaire_id, equipement_id, quantite, observation)
                                    VALUES (@inv, @eq, @qte, @obs);";
                                cmd.Parameters.AddWithValue("@inv", inventaireId);
                                cmd.Parameters.AddWithValue("@eq", ligne.EquipementId);
                                cmd.Parameters.AddWithValue("@qte", ligne.Quantite);
                                cmd.Parameters.AddWithValue("@obs", (object?)ligne.Observation ?? DBNull.Value);
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
                            "Cause probable : un équipement référencé a été supprimé entre-temps.\n\n" +
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

            InventaireEnregistre = true;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}