using Guna.UI2.WinForms;
using InventoryApp.Data;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace VotreAppNamespace
{
    public class FrmAccueil : Form
    {
        // Événements pour rendre les boutons fonctionnels
        public event EventHandler OnNouveauMouvementClicked;
        public event EventHandler OnAjouterEquipementClicked;
        public event EventHandler OnAjouterEmployeClicked;
        public event EventHandler OnInventaireClicked;

        // Éléments du Header Animé
        private Guna2Panel panelHeader;
        private Label lblTitreAnime;
        private System.Windows.Forms.Timer timerHeaderAnimation;
        private int posXText;
        private const string TEXTE_HEADER = "Ministère de l'Intérieur, des Collectivités Locales et des Transports — Direction Générale des Transmissions Nationales — Direction des Transmissions Nationales de la Wilaya de Relizane";

        // KPI Cards
        private TableLayoutPanel layoutCards;
        private Guna2Panel cardTotal, cardStock, cardAffecte, cardPanne;
        private Label lblTotalNum, lblStockNum, lblAffecteNum, lblPanneNum;

        // Boutons d'action
        private TableLayoutPanel layoutActions;

        // Section Camembert (Pie-Chart Dessiné) + ComboBox
        private Guna2Panel panelChartSection;
        private Guna2ComboBox cbCategories;
        private Panel panelPieChartDisplay;
        private DataTable dtChartData;

        // Double Grille
        private TableLayoutPanel layoutTables;
        private Guna2DataGridView gridMouvements;
        private Guna2DataGridView gridEmployes;

        public FrmAccueil()
        {
            InitializeComponent();
            DémarrerAnimationHeader();
            ChargerStatistiques();
            ChargerCategoriesCombo();
            ChargerPieChartStatut(null);
            ChargerMouvementsRecents();
            ChargerResumeEmployes();
        }

        private void InitializeComponent()
        {
            this.TopLevel = false;
            this.FormBorderStyle = FormBorderStyle.None;
            this.Dock = DockStyle.Fill;
            this.BackColor = Color.FromArgb(242, 244, 247);
            this.Padding = new Padding(20);
            this.AutoScroll = true;

            // ==========================================
            // 1. EN-TÊTE AVEC TITRE ANIMÉ
            // ==========================================
            panelHeader = new Guna2Panel
            {
                Dock = DockStyle.Top,
                Height = 45,
                FillColor = Color.FromArgb(30, 41, 59),
                BorderRadius = 8,
                Margin = new Padding(0, 0, 0, 15)
            };

            lblTitreAnime = new Label
            {
                Text = TEXTE_HEADER,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                BackColor = Color.Transparent,
                Top = 10
            };
            panelHeader.Controls.Add(lblTitreAnime);

            timerHeaderAnimation = new System.Windows.Forms.Timer { Interval = 25 };
            timerHeaderAnimation.Tick += TimerHeaderAnimation_Tick;

            // ==========================================
            // 2. CARTES STATISTIQUES (KPI)
            // ==========================================
            layoutCards = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 90,
                ColumnCount = 4,
                RowCount = 1,
                Margin = new Padding(0, 10, 0, 15)
            };
            layoutCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            layoutCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            layoutCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            layoutCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));

            cardTotal = CreerCarte("Total Équipements", "0", Color.FromArgb(37, 99, 235), out lblTotalNum);
            cardStock = CreerCarte("En Stock", "0", Color.FromArgb(16, 185, 129), out lblStockNum);
            cardAffecte = CreerCarte("Affectés / Prêt", "0", Color.FromArgb(245, 158, 11), out lblAffecteNum);
            cardPanne = CreerCarte("En Panne / Réparation", "0", Color.FromArgb(239, 68, 68), out lblPanneNum);

            layoutCards.Controls.Add(cardTotal, 0, 0);
            layoutCards.Controls.Add(cardStock, 1, 0);
            layoutCards.Controls.Add(cardAffecte, 2, 0);
            layoutCards.Controls.Add(cardPanne, 3, 0);

            // ==========================================
            // 3. BOUTONS D'ACCÈS RAPIDE
            // ==========================================
            layoutActions = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 50,
                ColumnCount = 4,
                RowCount = 1,
                Margin = new Padding(0, 0, 0, 15)
            };
            layoutActions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            layoutActions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            layoutActions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            layoutActions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));

            Guna2Button btnNouveauMouvement = CreerBoutonAction("+ Nouveau Mouvement", Color.FromArgb(37, 99, 235));
            btnNouveauMouvement.Click += (s, e) => OnNouveauMouvementClicked?.Invoke(this, EventArgs.Empty);

            Guna2Button btnAjouterEquipement = CreerBoutonAction("+ Ajouter Équipement", Color.FromArgb(16, 185, 129));
            btnAjouterEquipement.Click += (s, e) => OnAjouterEquipementClicked?.Invoke(this, EventArgs.Empty);

            Guna2Button btnAjouterEmploye = CreerBoutonAction("+ Ajouter Employé", Color.FromArgb(107, 114, 128));
            btnAjouterEmploye.Click += (s, e) => OnAjouterEmployeClicked?.Invoke(this, EventArgs.Empty);

            Guna2Button btnInventaire = CreerBoutonAction("Lancer Inventaire", Color.FromArgb(139, 92, 246));
            btnInventaire.Click += (s, e) => OnInventaireClicked?.Invoke(this, EventArgs.Empty);

            layoutActions.Controls.Add(btnNouveauMouvement, 0, 0);
            layoutActions.Controls.Add(btnAjouterEquipement, 1, 0);
            layoutActions.Controls.Add(btnAjouterEmploye, 2, 0);
            layoutActions.Controls.Add(btnInventaire, 3, 0);

            // ==========================================
            // 4. SECTION PIE-CHART DESSINÉE (RÉPARTITION)
            // ==========================================
            panelChartSection = new Guna2Panel
            {
                Dock = DockStyle.Top,
                Height = 280,
                FillColor = Color.White,
                BorderRadius = 10,
                Margin = new Padding(0, 0, 0, 15),
                Padding = new Padding(15)
            };

            Panel headerChartPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 40
            };

            Label lblChartTitle = new Label
            {
                Text = "Répartition des Équipements par Statut",
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Dock = DockStyle.Left,
                AutoSize = true
            };

            cbCategories = new Guna2ComboBox
            {
                Dock = DockStyle.Right,
                Width = 250,
                BorderRadius = 6,
                Font = new Font("Segoe UI", 9.5F),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cbCategories.SelectedIndexChanged += CbCategories_SelectedIndexChanged;

            headerChartPanel.Controls.Add(lblChartTitle);
            headerChartPanel.Controls.Add(cbCategories);

            panelPieChartDisplay = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White
            };
            panelPieChartDisplay.Paint += PanelPieChartDisplay_Paint;

            panelChartSection.Controls.Add(panelPieChartDisplay);
            panelChartSection.Controls.Add(headerChartPanel);

            // ==========================================
            // 5. DOUBLE GRILLE (MOUVEMENTS & EMPLOYES)
            // ==========================================
            layoutTables = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 300,
                ColumnCount = 2,
                RowCount = 1,
                Margin = new Padding(0, 0, 0, 15)
            };
            layoutTables.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            layoutTables.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

            gridMouvements = CreerDataGridView();
            Guna2Panel panelMouvements = CreerConteneurGrille("Mouvements Récents (Affectations / Prêts)", gridMouvements);

            gridEmployes = CreerDataGridView();
            Guna2Panel panelEmployes = CreerConteneurGrille("Équipements Affectés par Employé", gridEmployes);

            layoutTables.Controls.Add(panelMouvements, 0, 0);
            layoutTables.Controls.Add(panelEmployes, 1, 0);

            // Assemblage global
            this.Controls.Add(layoutTables);
            this.Controls.Add(panelChartSection);
            this.Controls.Add(layoutActions);
            this.Controls.Add(layoutCards);
            this.Controls.Add(panelHeader);
        }

        // --- ANIMATION HEADER (Marquee) ---
        private void DémarrerAnimationHeader()
        {
            posXText = panelHeader.Width;
            lblTitreAnime.Left = posXText;
            timerHeaderAnimation.Start();

            panelHeader.SizeChanged += (s, e) =>
            {
                if (lblTitreAnime.Left > panelHeader.Width)
                    posXText = panelHeader.Width;
            };
        }

        private void TimerHeaderAnimation_Tick(object sender, EventArgs e)
        {
            posXText -= 2;
            if (posXText + lblTitreAnime.Width < 0)
            {
                posXText = panelHeader.Width;
            }
            lblTitreAnime.Left = posXText;
        }

        // --- CHARGEMENT DU PIE CHART PAR DESSIN AVEC COULEURS ---
        private void ChargerCategoriesCombo()
        {
            string sql = "SELECT id, designation FROM Categorie ORDER BY designation";
            DataTable dt = DatabaseHelper.ExecuteQuery(sql);

            DataTable dtCombo = new DataTable();
            dtCombo.Columns.Add("id", typeof(int));
            dtCombo.Columns.Add("designation", typeof(string));

            dtCombo.Rows.Add(-1, "-- Toutes les catégories --");
            foreach (DataRow dr in dt.Rows)
            {
                dtCombo.Rows.Add(dr["id"], dr["designation"]);
            }

            cbCategories.DataSource = dtCombo;
            cbCategories.DisplayMember = "designation";
            cbCategories.ValueMember = "id";
            cbCategories.SelectedIndex = 0;
        }

        private void CbCategories_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cbCategories.SelectedValue != null && int.TryParse(cbCategories.SelectedValue.ToString(), out int catId))
            {
                ChargerPieChartStatut(catId == -1 ? (int?)null : catId);
            }
        }

        
        private void PanelPieChartDisplay_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            if (dtChartData == null || dtChartData.Rows.Count == 0)
            {
                using (Font font = new Font("Segoe UI", 10F, FontStyle.Italic))
                using (SolidBrush brush = new SolidBrush(Color.Gray))
                {
                    e.Graphics.DrawString("Aucun équipement trouvé pour cette catégorie.", font, brush, new PointF(20, 50));
                }
                return;
            }

            int totalGlobal = 0;
            foreach (DataRow r in dtChartData.Rows)
            {
                totalGlobal += Convert.ToInt32(r["Total"]);
            }

            if (totalGlobal == 0) return;

            Color[] palette = new Color[]
            {
                Color.FromArgb(16, 185, 129), // Vert
                Color.FromArgb(245, 158, 11), // Orange
                Color.FromArgb(239, 68, 68),  // Rouge
                Color.FromArgb(59, 130, 246), // Bleu
                Color.FromArgb(107, 114, 128) // Gris
            };

            float startAngle = 0;
            int rectSize = Math.Min(panelPieChartDisplay.Height - 40, 200);
            Rectangle chartRect = new Rectangle(20, (panelPieChartDisplay.Height - rectSize) / 2, rectSize, rectSize);

            int legendX = rectSize + 60;
            int legendY = 30;

            for (int i = 0; i < dtChartData.Rows.Count; i++)
            {
                DataRow row = dtChartData.Rows[i];
                string statut = row["Statut"].ToString();
                int count = Convert.ToInt32(row["Total"]);

                float sweepAngle = (count / (float)totalGlobal) * 360f;
                Color itemColor = palette[i % palette.Length];

                using (SolidBrush brush = new SolidBrush(itemColor))
                {
                    e.Graphics.FillPie(brush, chartRect, startAngle, sweepAngle);

                    // Dessin de la légende
                    e.Graphics.FillRectangle(brush, legendX, legendY + (i * 30), 16, 16);
                }

                using (Font font = new Font("Segoe UI", 9.5F, FontStyle.Regular))
                using (SolidBrush textBrush = new SolidBrush(Color.FromArgb(30, 41, 59)))
                {
                    double pct = Math.Round((count / (double)totalGlobal) * 100, 1);
                    string legendText = $"{statut} : {count} ({pct}%)";
                    e.Graphics.DrawString(legendText, font, textBrush, legendX + 25, legendY + (i * 30) - 2);
                }

                startAngle += sweepAngle;
            }
        }

        // --- METHODES DE DECORATION ET REQUETES ---
        private Guna2Panel CreerCarte(string titre, string valeurInitiale, Color couleurAccent, out Label lblValeur)
        {
            Guna2Panel card = new Guna2Panel
            {
                Dock = DockStyle.Fill,
                BorderRadius = 10,
                FillColor = Color.White,
                Margin = new Padding(5, 0, 5, 0),
                Padding = new Padding(12, 10, 12, 10)
            };

            Guna2Panel accentBar = new Guna2Panel
            {
                Width = 5,
                Dock = DockStyle.Left,
                FillColor = couleurAccent,
                BorderRadius = 2
            };

            Panel contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10, 0, 0, 0)
            };

            Label lblTitreCard = new Label
            {
                Text = titre,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                ForeColor = Color.Gray,
                Dock = DockStyle.Top,
                AutoSize = true
            };

            lblValeur = new Label
            {
                Text = valeurInitiale,
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Dock = DockStyle.Bottom,
                AutoSize = true
            };

            contentPanel.Controls.Add(lblValeur);
            contentPanel.Controls.Add(lblTitreCard);

            card.Controls.Add(contentPanel);
            card.Controls.Add(accentBar);

            return card;
        }

        private Guna2Button CreerBoutonAction(string texte, Color couleurFond)
        {
            return new Guna2Button
            {
                Text = texte,
                FillColor = couleurFond,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                BorderRadius = 8,
                Dock = DockStyle.Fill,
                Margin = new Padding(5, 0, 5, 0),
                Cursor = Cursors.Hand
            };
        }

        private Guna2DataGridView CreerDataGridView()
        {
            Guna2DataGridView dgv = new Guna2DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                EnableHeadersVisualStyles = false
            };

            dgv.ThemeStyle.HeaderStyle.BackColor = Color.FromArgb(30, 41, 59);
            dgv.ThemeStyle.HeaderStyle.ForeColor = Color.White;
            dgv.ThemeStyle.HeaderStyle.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);

            dgv.ThemeStyle.RowsStyle.Font = new Font("Segoe UI", 9F);
            dgv.ThemeStyle.RowsStyle.SelectionBackColor = Color.FromArgb(239, 246, 255);
            dgv.ThemeStyle.RowsStyle.SelectionForeColor = Color.FromArgb(30, 41, 59);

            return dgv;
        }

        private Guna2Panel CreerConteneurGrille(string titreSection, DataGridView dgv)
        {
            Guna2Panel panel = new Guna2Panel
            {
                Dock = DockStyle.Fill,
                BorderRadius = 10,
                FillColor = Color.White,
                Margin = new Padding(5),
                Padding = new Padding(12)
            };

            Label lblTitre = new Label
            {
                Text = titreSection,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Dock = DockStyle.Top,
                Height = 30
            };

            panel.Controls.Add(dgv);
            panel.Controls.Add(lblTitre);

            return panel;
        }

        public void ChargerStatistiques()
        {
            string sql = @"
                SELECT 
                    COUNT(*) AS Total,
                    COUNT(CASE WHEN statut = 'En stock' THEN 1 END) AS EnStock,
                    COUNT(CASE WHEN statut IN ('Affecté', 'En prêt') THEN 1 END) AS Affectes,
                    COUNT(CASE WHEN statut IN ('En panne', 'En réparation') THEN 1 END) AS EnPanne
                FROM Equipement";

            DataTable dt = DatabaseHelper.ExecuteQuery(sql);
            if (dt.Rows.Count > 0)
            {
                DataRow dr = dt.Rows[0];
                lblTotalNum.Text = dr["Total"].ToString();
                lblStockNum.Text = dr["EnStock"].ToString();
                lblAffecteNum.Text = dr["Affectes"].ToString();
                lblPanneNum.Text = dr["EnPanne"].ToString();
            }
        }

        public void ChargerMouvementsRecents()
        {
            string sql = @"
                SELECT 
                    m.code_mouvement AS 'Code',
                    m.type_mouvement AS 'Type',
                    COALESCE(e.nom || ' ' || e.prenom, '—') AS 'Bénéficiaire',
                    m.date_mouvement AS 'Date'
                FROM Mouvement m
                LEFT JOIN Employe e ON m.employe_id = e.id
                ORDER BY m.id DESC
                LIMIT 8";

            gridMouvements.DataSource = DatabaseHelper.ExecuteQuery(sql);
        }

        public void ChargerResumeEmployes()
        {
            string sql = @"
                SELECT 
                    e.matricule AS 'Matricule',
                    e.nom || ' ' || e.prenom AS 'Employé',
                    e.departement AS 'Département',
                    COUNT(lm.equipement_id) AS 'Nb Équipements'
                FROM Employe e
                LEFT JOIN Mouvement m ON m.employe_id = e.id AND m.type_mouvement IN ('Affectation', 'Prêt')
                LEFT JOIN Ligne_mouvement lm ON lm.mouvement_id = m.id AND lm.est_sortie = 1
                GROUP BY e.id
                ORDER BY 'Nb Équipements' DESC
                LIMIT 8";

            gridEmployes.DataSource = DatabaseHelper.ExecuteQuery(sql);
        }

        private void ChargerPieChartStatut(int? categorieId)
        {
            string sql = @"
        SELECT 
            e.statut AS Statut, 
            COUNT(e.id) AS Total 
        FROM Equipement e
        JOIN Modele m ON e.modele_id = m.id
        WHERE (@CategorieId IS NULL OR m.categorie_id = @CategorieId)
        GROUP BY e.statut";

            var parameters = new System.Collections.Generic.Dictionary<string, object>
    {
        { "@CategorieId", (object)categorieId ?? DBNull.Value }
    };

            dtChartData = DatabaseHelper.ExecuteQueryWithParams(sql, parameters);
            panelPieChartDisplay.Invalidate(); // Redessiner le graphique
        }
    }
}