using Guna.UI2.WinForms;
using InventoryApp.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace InventoryApp
{
    public class FrmAccueil : Form
    {
        // Events utilisés par Form1
        public event EventHandler OnNouveauMouvementClicked;
        public event EventHandler OnAjouterEmployeClicked;
        public event EventHandler OnInventaireClicked;
        public event EventHandler<int> OnEditEmployeRequested;
        public event EventHandler<int> OnDeleteEmployeRequested;

        private readonly Form1 _mainForm;

        // Couleurs
        private readonly Color couleurFond = Color.FromArgb(245, 247, 250);
        private readonly Color couleurTexte = Color.FromArgb(30, 41, 59);

        // Header
        private Guna2Panel panelHeader;
        private Label lblTitreAnime;
        private System.Windows.Forms.Timer timerHeaderAnimation;
        private int posXText;

        private const string TEXTE_HEADER = " République Algérienne Démocratique et Populaire __ Ministère de l'Intérieur, des Collectivités Locales et des Transports __ Direction Générale des Transmissions Nationales __ Direction des Transmissions Nationales de la Wilaya de Relizane     الجمهورية الجزائرية الديمقراطية الشعبية __ وزارة الداخلية والجماعات المحلية والتهيئة العمرانية __ المديرية العامة للمواصلات السلكية واللاسلكيةالوطنية __ مديرية المواصلات السلكية واللاسلكيةالوطنية لولاية غليزان";
        // Cartes statistiques
        private TableLayoutPanel layoutCards;
        private Label lblModelesNum;
        private Label lblStockNum;
        private Label lblCirculationNum;
        private Label lblAlerteNum;

        // Toolbar
        private Guna2Panel panelToolbar;
        private Guna2TextBox txtSearch;
        private FlowLayoutPanel flowActions;
        private Guna2Button btnNewMovement;
        private Guna2Button btnQuickAddModele;
        private Guna2Button btnQuickAddEmp;
        private Guna2Button btnInventory;

        // Graphique
        private Guna2Panel panelChartSection;
        private Label lblChartTitle;
        private Guna2ComboBox cbCategories;
        private Panel panelPieChartDisplay;
        private DataTable dtChartData;

        // Tableaux
        private TableLayoutPanel layoutTables;
        private Guna2DataGridView gridMouvements;
        private Guna2DataGridView gridEmployes;

        // Footer
        private Guna2Panel panelFooter;
        private Label lblConnectionStatus;
        private Label lblDateTime;
        private System.Windows.Forms.Timer timerClock;

        public FrmAccueil(Form1 mainForm)
        {
            _mainForm = mainForm;

            InitializeComponent();

            StartHeaderAnimation();
            StartClock();

            ChargerStatistiques();
            ChargerCategoriesCombo();
            ChargerPieChartStock(null);
            ChargerQTEAlert();
            ChargerResumeEmployes();
            UpdateDbConnectionStatus();
        }

        private void InitializeComponent()
        {
            SuspendLayout();

            // Formulaire enfant dans home_container
            TopLevel = false;
            FormBorderStyle = FormBorderStyle.None;
            Dock = DockStyle.Fill;

            BackColor = couleurFond;
            Padding = new Padding(12);
            AutoScroll = true;
            AutoScaleMode = AutoScaleMode.Dpi;
            RightToLeft = RightToLeft.No;
            RightToLeftLayout = false;

            // ================= HEADER =================
            panelHeader = new Guna2Panel
            {
                Dock = DockStyle.Top,
                Height = 52,
                FillColor = Color.FromArgb(15, 23, 42),
                BorderRadius = 10,
                Margin = new Padding(0, 0, 0, 10)
            };

            panelHeader.ShadowDecoration.Enabled = true;
            panelHeader.ShadowDecoration.Depth = 5;
            panelHeader.ShadowDecoration.Color = Color.FromArgb(50, 0, 0, 0);

            lblTitreAnime = new Label
            {
                Text = TEXTE_HEADER,
                AutoSize = true,
                BackColor = Color.Transparent,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Top = 16,
                Left = 0
            };

            panelHeader.Controls.Add(lblTitreAnime);

            timerHeaderAnimation = new System.Windows.Forms.Timer
            {
                Interval = 25
            };
            timerHeaderAnimation.Tick += TimerHeaderAnimation_Tick;

            // ================= CARTES KPI =================
            layoutCards = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 105,
                ColumnCount = 4,
                RowCount = 1,
                Margin = new Padding(0, 0, 0, 10),
                Padding = new Padding(0)
            };

            layoutCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            layoutCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            layoutCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            layoutCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));

            layoutCards.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            Guna2Panel cardTotal = CreerCarte(
                "Articles référencés",
                "0",
                Color.FromArgb(59, 130, 246),
                out lblModelesNum);

            Guna2Panel cardStock = CreerCarte(
                "Quantité en stock",
                "0",
                Color.FromArgb(16, 185, 129),
                out lblStockNum);

            Guna2Panel cardAffecte = CreerCarte(
                "Affectés / Prêts (qté)",
                "0",
                Color.FromArgb(245, 158, 11),
                out lblCirculationNum);

            Guna2Panel cardPanne = CreerCarte(
                "Articles en alerte de stock",
                "0",
                Color.FromArgb(239, 68, 68),
                out lblAlerteNum);

            layoutCards.Controls.Add(cardTotal, 0, 0);
            layoutCards.Controls.Add(cardStock, 1, 0);
            layoutCards.Controls.Add(cardAffecte, 2, 0);
            layoutCards.Controls.Add(cardPanne, 3, 0);

            // ================= TOOLBAR =================
            panelToolbar = new Guna2Panel
            {
                Dock = DockStyle.Top,
                Height = 64,
                FillColor = Color.White,
                BorderRadius = 10,
                Margin = new Padding(0, 0, 0, 10),
                Padding = new Padding(10)
            };

            panelToolbar.ShadowDecoration.Enabled = true;
            panelToolbar.ShadowDecoration.Depth = 4;
            panelToolbar.ShadowDecoration.Color = Color.FromArgb(35, 0, 0, 0);

            txtSearch = new Guna2TextBox
            {
                PlaceholderText = "Recherche : nom, id, département...",
                Dock = DockStyle.Left,
                Width = 290,
                BorderRadius = 8,
                Font = new Font("Segoe UI", 9F),
                Margin = new Padding(0)
            };
            txtSearch.TextChanged += TxtSearch_TextChanged;

            flowActions = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                Width = 560,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                Padding = new Padding(0),
                Margin = new Padding(0)
            };

            btnNewMovement = CreerBoutonAction(
                "+ Mouvement",
                Color.FromArgb(59, 130, 246));

            btnQuickAddModele = CreerBoutonAction(
                "+ Article",
                Color.FromArgb(16, 185, 129));

            btnQuickAddEmp = CreerBoutonAction(
                "+ Employé",
                Color.FromArgb(100, 116, 139));

            btnInventory = CreerBoutonAction(
                "Inventaire",
                Color.FromArgb(139, 92, 246));

            btnNewMovement.Click += BtnNewMovement_Click;
            btnQuickAddModele.Click += BtnQuickAddModele_Click;
            btnQuickAddEmp.Click += BtnQuickAddEmp_Click;
            btnInventory.Click += BtnInventory_Click;

            flowActions.Controls.Add(btnInventory);
            flowActions.Controls.Add(btnQuickAddEmp);
            flowActions.Controls.Add(btnQuickAddModele);
            flowActions.Controls.Add(btnNewMovement);

            panelToolbar.Controls.Add(flowActions);
            panelToolbar.Controls.Add(txtSearch);

            // ================= GRAPHIQUE =================
            panelChartSection = new Guna2Panel
            {
                Dock = DockStyle.Top,
                Height = 285,
                FillColor = Color.White,
                BorderRadius = 10,
                Margin = new Padding(0, 0, 0, 10),
                Padding = new Padding(12)
            };

            panelChartSection.ShadowDecoration.Enabled = true;
            panelChartSection.ShadowDecoration.Depth = 4;
            panelChartSection.ShadowDecoration.Color = Color.FromArgb(35, 0, 0, 0);

            Panel chartHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 40
            };

            lblChartTitle = new Label
            {
                Text = "Répartition du stock par catégorie",
                Dock = DockStyle.Left,
                AutoSize = true,
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                ForeColor = couleurTexte,
                TextAlign = ContentAlignment.MiddleLeft
            };

            cbCategories = new Guna2ComboBox
            {
                Dock = DockStyle.Right,
                Width = 240,
                BorderRadius = 7,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9F)
            };
            cbCategories.SelectedIndexChanged += CbCategories_SelectedIndexChanged;

            chartHeader.Controls.Add(cbCategories);
            chartHeader.Controls.Add(lblChartTitle);

            panelPieChartDisplay = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White
            };
            panelPieChartDisplay.Paint += PanelPieChartDisplay_Paint;
            panelPieChartDisplay.Resize += (s, e) => panelPieChartDisplay.Invalidate();

            panelChartSection.Controls.Add(panelPieChartDisplay);
            panelChartSection.Controls.Add(chartHeader);

            // ================= TABLEAUX =================
            layoutTables = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 330,
                ColumnCount = 2,
                RowCount = 1,
                Margin = new Padding(0, 0, 0, 10),
                Padding = new Padding(0)
            };

            layoutTables.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            layoutTables.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            layoutTables.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            gridMouvements = CreerDataGridView();
            gridEmployes = CreerDataGridView();

            PrepareEmployesGrid(gridEmployes);

            Guna2Panel panelMouvements = CreerConteneurGrille(
                "Alerte de QTE",
                gridMouvements);

            Guna2Panel panelEmployes = CreerConteneurGrille(
                "Liste des employés",
                gridEmployes);

            layoutTables.Controls.Add(panelMouvements, 0, 0);
            layoutTables.Controls.Add(panelEmployes, 1, 0);

            // ================= FOOTER =================
            panelFooter = new Guna2Panel
            {
                Dock = DockStyle.Bottom,
                Height = 34,
                FillColor = Color.White,
                BorderRadius = 8,
                Padding = new Padding(10, 2, 10, 2)
            };

            lblConnectionStatus = new Label
            {
                Text = "Connexion DB : ...",
                Dock = DockStyle.Left,
                Width = 180,
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.DimGray,
                TextAlign = ContentAlignment.MiddleLeft
            };

            lblDateTime = new Label
            {
                Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                Dock = DockStyle.Right,
                Width = 180,
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.DimGray,
                TextAlign = ContentAlignment.MiddleRight
            };

            panelFooter.Controls.Add(lblDateTime);
            panelFooter.Controls.Add(lblConnectionStatus);

            timerClock = new System.Windows.Forms.Timer
            {
                Interval = 1000
            };
            timerClock.Tick += TimerClock_Tick;

            // Important : ordre des Controls
            Controls.Add(panelFooter);
            Controls.Add(layoutTables);
            Controls.Add(panelChartSection);
            Controls.Add(panelToolbar);
            Controls.Add(layoutCards);
            Controls.Add(panelHeader);

            //gridEmployes.CellContentClick += GridEmployes_CellContentClick;
            gridEmployes.CellMouseEnter += GridEmployes_CellMouseEnter;
            gridEmployes.CellMouseLeave += GridEmployes_CellMouseLeave;

            ResumeLayout(false);


        }

        // ================= ACTIONS =================

        private void BtnNewMovement_Click(object sender, EventArgs e)
        {
            // Form1 (abonné à cet événement) ouvre FrmAjouterMouvement, puis rafraîchit ses grilles
            // ET ce tableau de bord. Ne rien ouvrir ici : sinon le formulaire s'afficherait deux fois.
            OnNouveauMouvementClicked?.Invoke(this, EventArgs.Empty);
        }

        private void BtnQuickAddModele_Click(object sender, EventArgs e)
        {
            using (FrmAjouterModele frm = new FrmAjouterModele())
            {
                if (frm.ShowDialog(this) == DialogResult.OK)
                    RafraichirDashboard();
            }
        }

        private void BtnQuickAddEmp_Click(object sender, EventArgs e)
        {
            OnAjouterEmployeClicked?.Invoke(this, EventArgs.Empty);

            using (FrmAjouterEmploye frm = new FrmAjouterEmploye())
            {
                if (frm.ShowDialog(this) == DialogResult.OK)
                    RafraichirDashboard();
            }
        }

        private void BtnInventory_Click(object sender, EventArgs e)
        {
            // Même principe que pour le mouvement : c'est Form1 qui gère l'ouverture de l'inventaire.
            OnInventaireClicked?.Invoke(this, EventArgs.Empty);
        }

        public void RafraichirDashboard()
        {
            ChargerStatistiques();
            ChargerCategoriesCombo();
            ChargerPieChartStock(null);
            ChargerQTEAlert();
            ChargerResumeEmployes();
            UpdateDbConnectionStatus();
        }

        // ================= HEADER =================

        private void StartHeaderAnimation()
        {
            panelHeader.SizeChanged += PanelHeader_SizeChanged;

            posXText = panelHeader.Width;
            lblTitreAnime.Left = posXText;

            timerHeaderAnimation.Start();
        }

        private void PanelHeader_SizeChanged(object sender, EventArgs e)
        {
            if (lblTitreAnime.Left > panelHeader.Width)
            {
                posXText = panelHeader.Width;
                lblTitreAnime.Left = posXText;
            }
        }

        private void TimerHeaderAnimation_Tick(object sender, EventArgs e)
        {
            posXText -= 2;

            if (posXText + lblTitreAnime.Width < 0)
                posXText = panelHeader.Width;

            lblTitreAnime.Left = posXText;
        }

        // ================= HORLOGE =================

        private void StartClock()
        {
            timerClock.Start();
        }

        private void TimerClock_Tick(object sender, EventArgs e)
        {
            lblDateTime.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }

        // ================= KPI =================

        public void ChargerStatistiques()
        {
            try
            {
                // Tout est calculé à partir de Modele (quantite / qte_alerte) et des lignes de mouvement.
                // "Affectés / Prêts" = par employé : quantités sorties en Affectation/Prêt moins quantités
                // rendues en Retour (jamais négatif), puis somme sur tous les employés.
                string sql = @"
                    SELECT
                        (SELECT COUNT(*) FROM Modele) AS Modeles,
                        (SELECT COALESCE(SUM(quantite), 0) FROM Modele) AS QteStock,
                        (SELECT COALESCE(SUM(net), 0) FROM (
                            SELECT MAX(0, SUM(CASE
                                    WHEN m.type_mouvement IN ('Affectation', 'Prêt') AND lm.est_sortie = 1 THEN lm.quantite
                                    WHEN m.type_mouvement = 'Retour' AND lm.est_sortie = 0 THEN -lm.quantite
                                    ELSE 0 END)) AS net
                            FROM Mouvement m
                            JOIN Ligne_mouvement lm ON lm.mouvement_id = m.id
                            WHERE m.employe_id IS NOT NULL
                            GROUP BY m.employe_id)) AS Circulation,
                        (SELECT COUNT(*) FROM Modele WHERE quantite <= qte_alerte) AS EnAlerte";

                DataTable dt = DatabaseHelper.ExecuteQuery(sql);

                if (dt == null || dt.Rows.Count == 0)
                    return;

                DataRow row = dt.Rows[0];

                lblModelesNum.Text = row["Modeles"] == DBNull.Value ? "0" : row["Modeles"].ToString();
                lblStockNum.Text = row["QteStock"] == DBNull.Value ? "0" : row["QteStock"].ToString();
                lblCirculationNum.Text = row["Circulation"] == DBNull.Value ? "0" : row["Circulation"].ToString();
                lblAlerteNum.Text = row["EnAlerte"] == DBNull.Value ? "0" : row["EnAlerte"].ToString();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Erreur statistiques : " + ex.Message);
            }
        }

        // ================= QTE ALERTe =================

        public void ChargerQTEAlert()
        {
            try
            {
                // Stock réel = Modele.quantite ; un modèle est en alerte dès que quantite <= qte_alerte
                // (les plus critiques en premier).
                string sql = @"SELECT 
                                md.id AS 'ID', 
                                md.reference AS 'Référence', 
                                md.designation AS 'Désignation', 
                                COALESCE(c.designation, '—') AS 'Catégorie', 
                                COALESCE(mq.designation, '—') AS 'Marque', 
                                md.qte_alerte AS 'QTE alerte',
                                md.quantite AS 'En stock'
                            FROM Modele md
                            LEFT JOIN Categorie c ON md.categorie_id = c.id
                            LEFT JOIN Marque mq ON md.marque_id = mq.id
                            WHERE md.quantite <= md.qte_alerte
                            ORDER BY md.quantite ASC, md.designation";

                // Repartir de zéro à chaque chargement : sans cela, si la 1re requête est vide (ou typée
                // autrement), les colonnes auto-générées gardent un mauvais type (image) et lèvent
                // "Invalid cast from String to Image" au rechargement suivant.
                gridMouvements.DataSource = null;
                gridMouvements.Columns.Clear();
                gridMouvements.AutoGenerateColumns = true;
                gridMouvements.DataSource = DatabaseHelper.ExecuteQuery(sql);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Erreur Alert de table de QTE d'alerte : " + ex.Message);
            }
        }

        // ================= EMPLOYÉS =================

        public void ChargerResumeEmployes()
        {
            try
            {
                string sql = @"
                    SELECT
                        e.id AS id,
                        (e.nom || ' ' || e.prenom) AS 'Employé',
                        COALESCE(e.function, 'Sans fonction') AS 'Fonction',
                        COALESCE(e.departement, 'Sans service') AS 'Département'
                    FROM Employe e
                    ORDER BY e.id DESC";

                DataTable dt = DatabaseHelper.ExecuteQuery(sql);

                if (dt != null)
                {
                    gridEmployes.DataSource = null;
                    gridEmployes.AutoGenerateColumns = true;
                    gridEmployes.DataSource = dt;
                    if (gridEmployes.Columns.Contains("Image"))
                        gridEmployes.Columns.Remove("Image");

                    if (gridEmployes.Columns.Contains("Photo"))
                        gridEmployes.Columns.Remove("Photo");

                    // 2. Définir l'ordre d'affichage exact (DisplayIndex)
                    int index = 0;
                    foreach (DataGridViewColumn col in gridEmployes.Columns)
                    {
                        if (col.Name != "colModifier" && col.Name != "colSupprimer")
                        {
                            col.DisplayIndex = index++;
                        }
                    }

                    // 3. Forcer les boutons à se placer TOUT À LA FIN (à droite)
                    if (gridEmployes.Columns.Contains("colModifier"))
                        gridEmployes.Columns["colModifier"].DisplayIndex = index++;

                    if (gridEmployes.Columns.Contains("colSupprimer"))
                        gridEmployes.Columns["colSupprimer"].DisplayIndex = index++;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Erreur employés : " + ex.Message);
            }
        }

        // ================= CATÉGORIES =================

        private void ChargerCategoriesCombo()
        {
            try
            {
                string sql = "SELECT id, designation FROM Categorie ORDER BY designation";
                DataTable dt = DatabaseHelper.ExecuteQuery(sql);

                DataTable dtCombo = new DataTable();
                dtCombo.Columns.Add("id", typeof(int));
                dtCombo.Columns.Add("designation", typeof(string));

                dtCombo.Rows.Add(-1, "-- Toutes les catégories --");

                if (dt != null)
                {
                    foreach (DataRow row in dt.Rows)
                        dtCombo.Rows.Add(row["id"], row["designation"]);
                }

                cbCategories.SelectedIndexChanged -= CbCategories_SelectedIndexChanged;

                cbCategories.DataSource = dtCombo;
                cbCategories.DisplayMember = "designation";
                cbCategories.ValueMember = "id";
                cbCategories.SelectedIndex = 0;

                cbCategories.SelectedIndexChanged += CbCategories_SelectedIndexChanged;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Erreur catégories : " + ex.Message);
            }
        }

        private void CbCategories_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cbCategories.SelectedValue == null)
                return;

            int categorieId;

            if (int.TryParse(cbCategories.SelectedValue.ToString(), out categorieId))
            {
                ChargerPieChartStock(categorieId == -1 ? (int?)null : categorieId);
            }
        }

        // ================= GRAPHIQUE =================

        // Toutes catégories : quantité en stock par catégorie.
        // Une catégorie choisie : quantité en stock par modèle de cette catégorie.
        // modele On garde les 5 plus grosses parts et on regroupe le reste dans "Autres" (palette de 6 couleurs).
        private void ChargerPieChartStock(int? categorieId)
        {
            try
            {
                DataTable source;

                if (categorieId.HasValue)
                {
                    string sqlModeles = @"
                        SELECT TRIM(m.designation || ' ' || COALESCE(m.reference, '')) AS Libelle,
                               m.quantite AS Total
                        FROM Modele m
                        WHERE m.categorie_id = @CategorieId AND m.quantite > 0
                        ORDER BY m.quantite DESC, m.designation";

                    source = DatabaseHelper.ExecuteQueryWithParams(sqlModeles,
                        new Dictionary<string, object> { { "@CategorieId", categorieId.Value } });
                    lblChartTitle.Text = "Répartition du stock par modèle";
                }
                else
                {
                    string sqlCategories = @"
                        SELECT COALESCE(c.designation, 'Sans catégorie') AS Libelle,
                               SUM(m.quantite) AS Total
                        FROM Modele m
                        LEFT JOIN Categorie c ON c.id = m.categorie_id
                        GROUP BY m.categorie_id
                        HAVING SUM(m.quantite) > 0
                        ORDER BY Total DESC";

                    source = DatabaseHelper.ExecuteQuery(sqlCategories);
                    lblChartTitle.Text = "Répartition du stock par catégorie";
                }

                dtChartData = RegrouperEnAutres(source, 5);
                panelPieChartDisplay.Invalidate();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Erreur graphique : " + ex.Message);
            }
        }

        private static DataTable RegrouperEnAutres(DataTable source, int maxParts)
        {
            DataTable resultat = new DataTable();
            resultat.Columns.Add("Libelle", typeof(string));
            resultat.Columns.Add("Total", typeof(long));

            if (source == null)
                return resultat;

            long autres = 0;
            int rang = 0;

            foreach (DataRow row in source.Rows)
            {
                long total = row["Total"] == DBNull.Value ? 0 : Convert.ToInt64(row["Total"]);

                if (rang < maxParts)
                    resultat.Rows.Add(row["Libelle"]?.ToString() ?? "?", total);
                else
                    autres += total;

                rang++;
            }

            if (autres > 0)
                resultat.Rows.Add("Autres", autres);

            return resultat;
        }

        private void PanelPieChartDisplay_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;

            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(panelPieChartDisplay.BackColor);

            if (dtChartData == null || dtChartData.Rows.Count == 0)
            {
                using (Font font = new Font("Segoe UI", 10F, FontStyle.Italic))
                using (Brush brush = new SolidBrush(Color.Gray))
                {
                    g.DrawString(
                        "Aucun stock à afficher.",
                        font,
                        brush,
                        new PointF(20, 40));
                }

                return;
            }

            int totalGlobal = 0;

            foreach (DataRow row in dtChartData.Rows)
            {
                if (row["Total"] != DBNull.Value)
                    totalGlobal += Convert.ToInt32(row["Total"]);
            }

            if (totalGlobal <= 0)
                return;

            Color[] palette =
            {
                Color.FromArgb(16, 185, 129),
                Color.FromArgb(59, 130, 246),
                Color.FromArgb(245, 158, 11),
                Color.FromArgb(239, 68, 68),
                Color.FromArgb(139, 92, 246),
                Color.FromArgb(100, 116, 139)
            };

            int espaceDisponible = Math.Min(
                panelPieChartDisplay.Width / 2,
                panelPieChartDisplay.Height - 20);

            int tailleGraphique = Math.Max(120, Math.Min(espaceDisponible, 210));

            Rectangle chartRect = new Rectangle(
                20,
                (panelPieChartDisplay.Height - tailleGraphique) / 2,
                tailleGraphique,
                tailleGraphique);

            int legendX = chartRect.Right + 30;
            int legendY = Math.Max(15, (panelPieChartDisplay.Height - (dtChartData.Rows.Count * 30)) / 2);

            float startAngle = -90F;

            for (int i = 0; i < dtChartData.Rows.Count; i++)
            {
                DataRow row = dtChartData.Rows[i];

                string libelle = row["Libelle"]?.ToString() ?? "?";
                int count = Convert.ToInt32(row["Total"]);

                float sweepAngle = (count / (float)totalGlobal) * 360F;
                Color couleur = palette[i % palette.Length];

                using (Brush brush = new SolidBrush(couleur))
                {
                    g.FillPie(brush, chartRect, startAngle, sweepAngle);
                    g.FillRectangle(brush, legendX, legendY + (i * 30), 16, 16);
                }

                double pourcentage = Math.Round((count / (double)totalGlobal) * 100, 1);

                using (Font font = new Font("Segoe UI", 9F))
                using (Brush brushText = new SolidBrush(couleurTexte))
                {
                    string texte = libelle + " : " + count + " (" + pourcentage + "%)";

                    g.DrawString(
                        texte,
                        font,
                        brushText,
                        legendX + 24,
                        legendY + (i * 30) - 2);
                }

                startAngle += sweepAngle;
            }
        }

        // ================= RECHERCHE =================

        private void TxtSearch_TextChanged(object sender, EventArgs e)
        {
            string q = txtSearch.Text.Trim();

            if (string.IsNullOrWhiteSpace(q))
            {
                ChargerResumeEmployes();
                return;
            }

            try
            {
                Dictionary<string, object> parameters = new Dictionary<string, object>
                {
                    { "@q", "%" + q + "%" }
                };

                string sqlEmp = @"
                    SELECT
                        e.id AS id,
                        e.matricule AS 'Matricule',
                        (e.nom || ' ' || e.prenom) AS 'Employé',
                        COALESCE(e.function, 'Sans fonction') AS 'Fonction',
                        COALESCE(e.departement, 'Sans service') AS 'Département',
                        MAX(0, COALESCE(SUM(CASE
                            WHEN m.type_mouvement IN ('Affectation', 'Prêt') AND lm.est_sortie = 1 THEN lm.quantite
                            WHEN m.type_mouvement = 'Retour' AND lm.est_sortie = 0 THEN -lm.quantite
                            ELSE 0 END), 0)) AS 'Qté détenue'
                    FROM Employe e
                    LEFT JOIN Mouvement m ON m.employe_id = e.id
                    LEFT JOIN Ligne_mouvement lm ON lm.mouvement_id = m.id
                    WHERE
                        (e.nom || ' ' || e.prenom) LIKE @q
                        OR e.departement LIKE @q
                    GROUP BY e.id
                    ORDER BY e.nom, e.prenom
                    LIMIT 200";

                gridEmployes.DataSource = null;
                gridEmployes.AutoGenerateColumns = true;
                gridEmployes.DataSource =
                    DatabaseHelper.ExecuteQueryWithParams(sqlEmp, parameters);


                if (gridEmployes.Columns.Contains("Image"))
                    gridEmployes.Columns.Remove("Image");

                if (gridEmployes.Columns.Contains("Photo"))
                    gridEmployes.Columns.Remove("Photo");

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Erreur recherche : " + ex.Message);
            }
        }

        // ================= GRILLE EMPLOYÉS =================

        private void PrepareEmployesGrid(Guna2DataGridView dgv)
        {
            dgv.AutoGenerateColumns = true;
            dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgv.MultiSelect = false;

            dgv.Columns.Clear();

            DataGridViewButtonColumn colModifier = new DataGridViewButtonColumn
            {
                Name = "colModifier",
                HeaderText = "Modifier",
                Width = 70,
                FlatStyle = FlatStyle.Flat,
                UseColumnTextForButtonValue = false
            };

            DataGridViewButtonColumn colSupprimer = new DataGridViewButtonColumn
            {
                Name = "colSupprimer",
                HeaderText = "Supprimer",
                Width = 80,
                FlatStyle = FlatStyle.Flat,
                UseColumnTextForButtonValue = false
            };

            dgv.Columns.Add(colModifier);
            dgv.Columns.Add(colSupprimer);

            dgv.CellPainting += EmployesGrid_CellPainting;
            dgv.CellClick += EmployesGrid_CellClick;

            dgv.CellMouseMove += (s, e) =>
            {
                if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
                    dgv.InvalidateCell(e.ColumnIndex, e.RowIndex);
            };

            dgv.CellMouseLeave += (s, e) =>
            {
                if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
                    dgv.InvalidateCell(e.ColumnIndex, e.RowIndex);
            };
        }
        private void EmployesGrid_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.Graphics == null) return;

            Guna2DataGridView? dgv = sender as Guna2DataGridView;
            if (dgv == null) return;

            Point mousePos = dgv.PointToClient(Cursor.Position);
            bool isHovered = e.CellBounds.Contains(mousePos);
            bool isClicked = isHovered && (Control.MouseButtons == MouseButtons.Left);

            // Bouton Modifier
            if (e.ColumnIndex == dgv.Columns["colModifier"]?.Index)
            {
                DessinerBouton(e, isHovered, isClicked,
                    Color.FromArgb(240, 253, 244),  // fond
                    Color.FromArgb(220, 252, 231),  // hover
                    Color.FromArgb(187, 247, 208),  // click
                    Color.FromArgb(134, 239, 172),  // bordure
                    "pencil_icon.png");             // icône
            }
            // Bouton Supprimer
            else if (e.ColumnIndex == dgv.Columns["colSupprimer"]?.Index)
            {
                DessinerBouton(e, isHovered, isClicked,
                    Color.FromArgb(254, 242, 242),
                    Color.FromArgb(254, 226, 226),
                    Color.FromArgb(254, 202, 202),
                    Color.FromArgb(252, 165, 165),
                    "delet_icon.png");
            }
        }

        private static void DessinerBouton(DataGridViewCellPaintingEventArgs e, bool isHovered, bool isClicked, Color bg, Color bgHover, Color bgClick, Color border, string iconFile)
        {
            if (e.Graphics == null) return;

            e.PaintBackground(e.CellBounds, true);

            Color cur = isClicked ? bgClick : (isHovered ? bgHover : bg);

            Rectangle rect = new Rectangle(
                e.CellBounds.Left + 4,
                e.CellBounds.Top + 4,
                e.CellBounds.Width - 8,
                e.CellBounds.Height - 8);

            using (SolidBrush brush = new SolidBrush(cur))
                e.Graphics.FillRectangle(brush, rect);

            using (Pen pen = new Pen(border))
                e.Graphics.DrawRectangle(pen, rect);

            // Même chemin que dans la fonction qui fonctionne
            string path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "image", iconFile);

            if (!System.IO.File.Exists(path))
                path = System.IO.Path.Combine("image", iconFile);

            if (System.IO.File.Exists(path))
            {
                using (Image img = Image.FromFile(path))
                {
                    int size = 18;
                    Rectangle iconRect = new Rectangle(
                        rect.Left + (rect.Width - size) / 2,
                        rect.Top + (rect.Height - size) / 2,
                        size, size);
                    e.Graphics.DrawImage(img, iconRect);
                }
            }

            e.Handled = true;
        }

        private void EmployesGrid_CellClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

            Guna2DataGridView dgv = sender as Guna2DataGridView;
            if (dgv == null || !dgv.Columns.Contains("id")) return;

            object valeurId = dgv.Rows[e.RowIndex].Cells["id"].Value;
            if (valeurId == null || valeurId == DBNull.Value) return;

            int idEmploye = Convert.ToInt32(valeurId);
            string colName = dgv.Columns[e.ColumnIndex].Name;

            if (colName == "colModifier")
            {
                OnEditEmployeRequested?.Invoke(this, idEmploye);
            }
            else if (colName == "colSupprimer")
            {
                string nomEmploye = dgv.Rows[e.RowIndex].Cells["Employé"].Value?.ToString() ?? "cet employé";

                DialogResult confirm = MessageBox.Show(
                    $"Supprimer définitivement {nomEmploye} ?",
                    "Confirmer la suppression",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (confirm == DialogResult.Yes)
                {
                    OnDeleteEmployeRequested?.Invoke(this, idEmploye);
                }
            }
        }

        /*private void GridEmployes_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

            DataGridView dgv = sender as DataGridView;
            if (dgv == null || !dgv.Columns.Contains("id")) return;

            object valeurId = dgv.Rows[e.RowIndex].Cells["id"].Value;
            if (valeurId == null || valeurId == DBNull.Value) return;

            int idEmploye = Convert.ToInt32(valeurId);
            string colonne = dgv.Columns[e.ColumnIndex].Name;

            if (colonne == "Edit")
            {
                OnEditEmployeRequested?.Invoke(this, idEmploye);
            }
            else if (colonne == "Delete")
            {
                DialogResult reponse = MessageBox.Show(
                    "Voulez-vous supprimer cet employé ?",
                    "Confirmation",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (reponse == DialogResult.Yes)
                {
                    OnDeleteEmployeRequested?.Invoke(this, idEmploye);
                }
            }
        }
       
        */

        private void GridEmployes_CellMouseEnter(object sender, DataGridViewCellEventArgs e)
        {
            DataGridView dgv = sender as DataGridView;

            if (dgv == null || e.RowIndex < 0)
                return;

            dgv.Rows[e.RowIndex].DefaultCellStyle.BackColor =
                Color.FromArgb(241, 245, 249);
        }

        private void GridEmployes_CellMouseLeave(object sender, DataGridViewCellEventArgs e)
        {
            DataGridView dgv = sender as DataGridView;

            if (dgv == null || e.RowIndex < 0)
                return;

            dgv.Rows[e.RowIndex].DefaultCellStyle.BackColor = Color.White;
        }

        // ================= AIDES UI =================

        private Guna2Panel CreerCarte(
            string titre,
            string valeurInitiale,
            Color couleurAccent,
            out Label lblValeur)
        {
            Guna2Panel card = new Guna2Panel
            {
                Dock = DockStyle.Fill,
                FillColor = Color.White,
                BorderRadius = 12,
                Margin = new Padding(5),
                Padding = new Padding(0)
            };

            card.ShadowDecoration.Enabled = true;
            card.ShadowDecoration.Depth = 4;
            card.ShadowDecoration.Color = Color.FromArgb(30, 0, 0, 0);

            Guna2Panel accent = new Guna2Panel
            {
                Dock = DockStyle.Left,
                Width = 6,
                FillColor = couleurAccent,
                BorderRadius = 3
            };

            Panel contenu = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(14, 10, 12, 10)
            };

            Label lblTitre = new Label
            {
                Text = titre,
                Dock = DockStyle.Top,
                AutoSize = true,
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(100, 116, 139)
            };

            lblValeur = new Label
            {
                Text = valeurInitiale,
                Dock = DockStyle.Bottom,
                AutoSize = true,
                Font = new Font("Segoe UI", 21F, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42)
            };

            contenu.Controls.Add(lblValeur);
            contenu.Controls.Add(lblTitre);

            card.Controls.Add(contenu);
            card.Controls.Add(accent);

            return card;
        }

        private Guna2Button CreerBoutonAction(string texte, Color couleurFond)
        {
            Guna2Button bouton = new Guna2Button
            {
                Text = texte,
                FillColor = couleurFond,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Height = 38,
                Width = 125,
                BorderRadius = 8,
                Margin = new Padding(4, 3, 4, 3),
                Cursor = Cursors.Hand
            };

            bouton.HoverState.FillColor = ControlPaint.Light(couleurFond);
            return bouton;
        }

        private Guna2DataGridView CreerDataGridView()
        {
            Guna2DataGridView dgv = new Guna2DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                AutoGenerateColumns = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RowHeadersVisible = false,
                CellBorderStyle = DataGridViewCellBorderStyle.None,
                MultiSelect = false
            };

            dgv.RowTemplate.Height = 38;

            dgv.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.White,
                ForeColor = Color.Black,
                SelectionBackColor = Color.FromArgb(239, 246, 255),
                SelectionForeColor = Color.Black
            };

            dgv.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                Alignment = DataGridViewContentAlignment.MiddleLeft,
                BackColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(37, 99, 235),
                SelectionBackColor = Color.White,
                SelectionForeColor = Color.FromArgb(37, 99, 235)
            };
            dgv.ColumnHeadersHeight = 40;

            dgv.DefaultCellStyle = new DataGridViewCellStyle
            {
                Alignment = DataGridViewContentAlignment.MiddleLeft,
                BackColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.Black,
                SelectionBackColor = Color.FromArgb(239, 246, 255),
                SelectionForeColor = Color.Black
            };

            // Événements pour le survol (déjà gérés dans PrepareEmployesGrid pour gridEmployes)
            dgv.CellMouseMove += (s, e) => dgv.InvalidateCell(e.ColumnIndex, e.RowIndex);
            dgv.CellMouseLeave += (s, e) => dgv.InvalidateCell(e.ColumnIndex, e.RowIndex);

            return dgv;
        }

        private Guna2Panel CreerConteneurGrille(string titre, DataGridView dgv)
        {
            Guna2Panel panel = new Guna2Panel
            {
                Dock = DockStyle.Fill,
                FillColor = Color.White,
                BorderRadius = 12,
                Margin = new Padding(5),
                Padding = new Padding(10)
            };

            panel.ShadowDecoration.Enabled = true;
            panel.ShadowDecoration.Depth = 4;
            panel.ShadowDecoration.Color = Color.FromArgb(30, 0, 0, 0);

            Label lblTitre = new Label
            {
                Text = titre,
                Dock = DockStyle.Top,
                Height = 34,
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                ForeColor = Color.Red,
                TextAlign = ContentAlignment.MiddleLeft
            };

            dgv.Dock = DockStyle.Fill;

            panel.Controls.Add(dgv);
            panel.Controls.Add(lblTitre);

            return panel;
        }

        // ================= CONNEXION DB =================

        public void UpdateDbConnectionStatus()
        {
            try
            {
                DatabaseHelper.ExecuteQuery("SELECT 1");

                lblConnectionStatus.Text = "● Connexion DB : OK";
                lblConnectionStatus.ForeColor = Color.FromArgb(16, 185, 129);
            }
            catch
            {
                lblConnectionStatus.Text = "● Connexion DB : Échec";
                lblConnectionStatus.ForeColor = Color.FromArgb(239, 68, 68);
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            timerHeaderAnimation?.Stop();
            timerClock?.Stop();

            timerHeaderAnimation?.Dispose();
            timerClock?.Dispose();

            base.OnFormClosed(e);
        }
    }
}