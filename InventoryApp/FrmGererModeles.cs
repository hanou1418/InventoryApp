#nullable enable
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows.Forms;
using Guna.UI2.WinForms;
using InventoryApp.Data;
using Microsoft.Data.Sqlite;

namespace InventoryApp
{
    public class FrmGererModeles : Form
    {
        private readonly Form1? _mainForm;

        public int? DernierModeleModifieId { get; private set; } = null;

        // Bandeau d'actions principal
        private Guna2Panel headerPanel = null!;
        private Guna2TextBox filtreTextBox = null!;
        private Guna2ComboBox listeFiltrageComboBox = null!;
        private Guna2HtmlLabel lblCompteur = null!;
        private FlowLayoutPanel rightActionsPanel = null!;
        private Guna2Button btnChoisirColonnes = null!;
        private Guna2Button btnImprimer = null!;
        private Guna2Button btnNouveauModele = null!;
        private Guna2DataGridView tableModelesDataGridView = null!;

        // Ligne de filtres par colonne (au-dessus de l'en-tête)
        private const int HauteurEntete = 78;        // zone titre (≈38) + zone filtres (≈40)
        private const int HauteurChampFiltre = 28;
        private Guna2Button btnViderFiltres = null!;
        private bool _effacementEnCours = false;
        private readonly Dictionary<string, Guna2TextBox> _filtresColonnes = new Dictionary<string, Guna2TextBox>();

        private static readonly (string Affichage, string Colonne)[] ColonnesFiltrablesModele = new[]
    {
        ("Tous les champs",    ""),
        ("ID",                 "ID"),
        ("Référence",          "Référence"),
        ("Désignation",        "Désignation"),
        ("Catégorie",          "Catégorie"),
        ("Marque",             "Marque"),
        ("Qté Actuelle",       "Qté Actuelle"),
        ("Entrées",            "Entrées"),
        ("Sorties",            "Sorties"),
        ("Emplacement",        "Emplacement"),
        ("Observation",        "Observation"),
        ("Dernier Mouvement",  "Dernier Mouvement"),
        ("Qté Alerte",         "Qté Alerte"),
        ("Date Ajout",         "Date Ajout")
    };

        public FrmGererModeles(Form1? mainForm)
        {
            _mainForm = mainForm;

            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.White;
            Dock = DockStyle.Fill;

            ConstruireControles();
            Load += (s, e) => ChargerListe();
        }

        private void ConstruireControles()
        {
            // 1. En-tête bleu sombre
            headerPanel = new Guna2Panel
            {
                Dock = DockStyle.Top,
                Height = 46,
                FillColor = Color.FromArgb(24, 30, 54)
            };

            // Zone de recherche
            filtreTextBox = new Guna2TextBox
            {
                Location = new Point(3, 5),
                Size = new Size(346, 36),
                BorderColor = Color.FromArgb(37, 99, 235),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(24, 30, 54),
                PlaceholderText = "Rechercher un Article . . . . .",
                IconRight = ChargerImageLocale("search.png"),
                IconRightSize = new Size(30, 30),
                IconRightOffset = new Point(0, 0)
            };
            filtreTextBox.TextChanged += (s, e) => AppliquerFiltre();

            // ComboBox de filtre
            listeFiltrageComboBox = new Guna2ComboBox
            {
                Location = new Point(352, 5),
                Size = new Size(151, 36),
                BackColor = Color.Transparent,
                BorderColor = Color.FromArgb(37, 99, 235),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(24, 30, 54)
            };
            foreach (var (affichage, _) in ColonnesFiltrablesModele)
                listeFiltrageComboBox.Items.Add(affichage);
            listeFiltrageComboBox.SelectedIndex = 0;
            listeFiltrageComboBox.SelectedIndexChanged += (s, e) => AppliquerFiltre();

            // Compteur
            lblCompteur = new Guna2HtmlLabel
            {
                Location = new Point(509, 12),
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.White,
                Text = "0 article(s)"
            };

            // Panneau conteneur aligné à droite pour les boutons d'actions
            rightActionsPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 5, 3, 0),
                WrapContents = false
            };

            // Bouton Sélection de colonnes
            btnChoisirColonnes = new Guna2Button
            {
                Size = new Size(63, 36),
                Margin = new Padding(0, 0, 5, 0),
                FillColor = Color.FromArgb(37, 99, 235),
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.White,
                Text = "Coll"
            };
            btnChoisirColonnes.Click += BtnChoisirColonnes_Click;

            // Bouton Impression générale
            btnImprimer = new Guna2Button
            {
                Size = new Size(38, 36),
                Margin = new Padding(0, 0, 5, 0),
                FillColor = Color.FromArgb(37, 99, 235),
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.White,
                Image = ChargerImageLocale("impriment_icon.png"),
                ImageSize = new Size(20, 20),
                ImageAlign = HorizontalAlignment.Center
            };
            btnImprimer.Click += BtnImprimer_Click;

            // Bouton Ajouter
            btnNouveauModele = new Guna2Button
            {
                Size = new Size(180, 36),
                Margin = new Padding(0, 0, 0, 0),
                FillColor = Color.FromArgb(37, 99, 235),
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.White,
                Text = "+ Ajouter un article"
            };
            btnNouveauModele.Click += BtnNouveauModele_Click;


            // Bouton : vider tous les filtres de colonnes
            btnViderFiltres = new Guna2Button
            {
                Size = new Size(38, 36),
                Margin = new Padding(0, 0, 5, 0),
                FillColor = Color.FromArgb(220, 38, 38),
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.White,
                Text = "✕"
            };
            new ToolTip().SetToolTip(btnViderFiltres, "Vider les filtres de colonnes");
            btnViderFiltres.Click += BtnViderFiltres_Click;
            rightActionsPanel.Controls.Add(btnViderFiltres);


            rightActionsPanel.Controls.Add(btnChoisirColonnes);
            rightActionsPanel.Controls.Add(btnImprimer);
            rightActionsPanel.Controls.Add(btnNouveauModele);

            headerPanel.Controls.Add(filtreTextBox);
            headerPanel.Controls.Add(listeFiltrageComboBox);
            headerPanel.Controls.Add(lblCompteur);
            headerPanel.Controls.Add(rightActionsPanel);

            // 2. DataGridView
            tableModelesDataGridView = new Guna2DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                AutoGenerateColumns = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RowHeadersVisible = false,
                CellBorderStyle = DataGridViewCellBorderStyle.None
            };

            tableModelesDataGridView.RowTemplate.Height = 38;

            tableModelesDataGridView.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.White,
                ForeColor = Color.Black,
                SelectionBackColor = Color.FromArgb(239, 246, 255),
                SelectionForeColor = Color.Black
            };

            tableModelesDataGridView.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                Alignment = DataGridViewContentAlignment.TopLeft,   // titre en haut, filtres en bas
                Padding = new Padding(4, 10, 0, 0),
                BackColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(37, 99, 235),
                SelectionBackColor = Color.White,
                SelectionForeColor = Color.FromArgb(37, 99, 235)
            };
            tableModelesDataGridView.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            tableModelesDataGridView.ColumnHeadersHeight = HauteurEntete;

            tableModelesDataGridView.DefaultCellStyle = new DataGridViewCellStyle
            {
                Alignment = DataGridViewContentAlignment.MiddleLeft,
                BackColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.Black,
                SelectionBackColor = Color.FromArgb(239, 246, 255),
                SelectionForeColor = Color.Black
            };

            tableModelesDataGridView.DataError += (s, e) => e.ThrowException = false;   // pas de boîte d'erreur par défaut
            tableModelesDataGridView.CellPainting += Dgv_CellPainting;
            tableModelesDataGridView.CellClick += Dgv_CellClick;
            tableModelesDataGridView.CellMouseMove += (s, e) => tableModelesDataGridView.InvalidateCell(e.ColumnIndex, e.RowIndex);
            tableModelesDataGridView.CellMouseLeave += (s, e) => tableModelesDataGridView.InvalidateCell(e.ColumnIndex, e.RowIndex);

            // Garder les champs alignés avec les colonnes (redimensionnement, défilement horizontal, réorganisation)
            tableModelesDataGridView.ColumnWidthChanged += (s, e) => PositionnerFiltresColonnes();
            tableModelesDataGridView.ColumnDisplayIndexChanged += (s, e) => PositionnerFiltresColonnes();
            tableModelesDataGridView.DataBindingComplete += (s, e) => PositionnerFiltresColonnes();
            tableModelesDataGridView.SizeChanged += (s, e) => PositionnerFiltresColonnes();
            tableModelesDataGridView.Scroll += (s, e) =>
            {
                if (e.ScrollOrientation == ScrollOrientation.HorizontalScroll) PositionnerFiltresColonnes();
            };

            Controls.Add(tableModelesDataGridView);
            Controls.Add(headerPanel);
        }

        public void ChargerListe()
        {
            string sql = @"
                            SELECT 
                                md.id AS 'ID', 
                                md.reference AS 'Référence',
                                COALESCE(c.designation, '—') AS 'Catégorie', 
                                COALESCE(mq.designation, '—') AS 'Marque',
                                md.designation AS 'Désignation', 

                                -- Quantité actuelle issue directement de l'attribut prêt dans Modele
                                md.quantite AS 'Qté Actuelle',

                                -- Calcul des entrées et sorties cumulées depuis Ligne_mouvement
                                COALESCE((
                                    SELECT SUM(lm.quantite) 
                                    FROM Ligne_mouvement lm 
                                    WHERE lm.modele_id = md.id AND lm.est_sortie = 0
                                ), 0) AS 'Entrées',

                                COALESCE((
                                    SELECT SUM(lm.quantite) 
                                    FROM Ligne_mouvement lm 
                                    WHERE lm.modele_id = md.id AND lm.est_sortie = 1
                                ), 0) AS 'Sorties',

                                -- Attributs existants dans la table Modele
                                COALESCE(md.emplacement, '—') AS 'Emplacement',
                                COALESCE(md.observation, '—') AS 'Observation',

                                -- Détails du dernier mouvement
                                COALESCE((
                                    SELECT m.type_mouvement || ' (' || m.date_mouvement || ')'
                                    FROM Ligne_mouvement lm
                                    INNER JOIN Mouvement m ON lm.mouvement_id = m.id
                                    WHERE lm.modele_id = md.id
                                    ORDER BY m.id DESC LIMIT 1
                                ), 'Aucun') AS 'Dernier Mouvement',
                                md.qte_alerte AS 'Qté Alerte',
                                md.date_creation AS 'Date Ajout'


                            FROM Modele md
                            LEFT JOIN Categorie c ON md.categorie_id = c.id
                            LEFT JOIN Marque mq ON md.marque_id = mq.id
                            ORDER BY md.id DESC";

            // Mémoriser les colonnes masquées via le bouton « Coll » (elles sont recréées au rechargement)
            var masquees = new HashSet<string>();
            foreach (DataGridViewColumn c in tableModelesDataGridView.Columns)
                if (!c.Visible && !EstColonneAction(c)) masquees.Add(c.Name);

            DataTable dt = CorrigerTypes(DatabaseHelper.ExecuteQuery(sql));
            tableModelesDataGridView.DataSource = dt;

            if (tableModelesDataGridView.Columns.Contains("colModifier")) tableModelesDataGridView.Columns.Remove("colModifier");
            if (tableModelesDataGridView.Columns.Contains("colSupprimer")) tableModelesDataGridView.Columns.Remove("colSupprimer");
            if (tableModelesDataGridView.Columns.Contains("colImprimer")) tableModelesDataGridView.Columns.Remove("colImprimer");

            tableModelesDataGridView.Columns.Add(new DataGridViewButtonColumn { Name = "colModifier", HeaderText = "Modifier", Width = 60, FlatStyle = FlatStyle.Flat });
            tableModelesDataGridView.Columns.Add(new DataGridViewButtonColumn { Name = "colSupprimer", HeaderText = "Supprimer", Width = 60, FlatStyle = FlatStyle.Flat });
            tableModelesDataGridView.Columns.Add(new DataGridViewButtonColumn { Name = "colImprimer", HeaderText = "Imprimer", Width = 60, FlatStyle = FlatStyle.Flat });

            foreach (string nom in masquees)
                if (tableModelesDataGridView.Columns.Contains(nom)) tableModelesDataGridView.Columns[nom].Visible = false;

            ReconstruireFiltresColonnes();
            PositionnerFiltresColonnes();
            AppliquerFiltre();
        }
        /// <summary>
        /// Les colonnes calculées de la requête (COALESCE, SUM, sous-requêtes) n'ont pas de type déclaré dans SQLite :
        /// le DataTable les type en byte[] et le DataGridView crée alors des colonnes IMAGE (⊠ + erreur
        /// « Invalid cast from String to Image »). On reconstruit un DataTable avec de vrais types.
        /// </summary>
        private static DataTable CorrigerTypes(DataTable source)
        {
            var colonnesNombres = new HashSet<string> { "Entrées", "Sorties" };

            var dt = new DataTable();
            foreach (DataColumn c in source.Columns)
            {
                Type t = c.DataType;
                if (t == typeof(byte[]) || t == typeof(object))
                    t = colonnesNombres.Contains(c.ColumnName) ? typeof(long) : typeof(string);
                dt.Columns.Add(c.ColumnName, t);
            }

            foreach (DataRow r in source.Rows)
            {
                DataRow nr = dt.NewRow();
                foreach (DataColumn c in dt.Columns)
                {
                    object v = r[c.ColumnName];
                    if (v == DBNull.Value) { nr[c.ColumnName] = DBNull.Value; continue; }
                    if (v is byte[] octets) v = Encoding.UTF8.GetString(octets);

                    if (c.DataType == typeof(long)) nr[c.ColumnName] = Convert.ToInt64(v, CultureInfo.InvariantCulture);
                    else if (c.DataType == typeof(string)) nr[c.ColumnName] = Convert.ToString(v, CultureInfo.InvariantCulture) ?? "";
                    else nr[c.ColumnName] = v;
                }
                dt.Rows.Add(nr);
            }
            return dt;
        }

        private static bool EstColonneAction(DataGridViewColumn c)
            => c.Name is "colModifier" or "colSupprimer" or "colImprimer";

        /// <summary>Minuscules + suppression des accents (é→e, à→a…) pour une recherche tolérante.</summary>
        private static string Normaliser(string? s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            string d = s.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(d.Length);
            foreach (char c in d)
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) sb.Append(c);
            return sb.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
        }

        /// <summary>Colonne choisie dans la liste de filtrage ("" = tous les champs).</summary>
        private string ColonneFiltreSelectionnee()
        {
            int i = listeFiltrageComboBox.SelectedIndex;
            return (i >= 0 && i < ColonnesFiltrablesModele.Length) ? ColonnesFiltrablesModele[i].Colonne : "";
        }

        // ───────────── FILTRES PAR COLONNE ─────────────

        /// <summary>Crée un champ de saisie par colonne de données (les textes déjà saisis sont conservés).</summary>
        private void ReconstruireFiltresColonnes()
        {
            var anciens = new Dictionary<string, string>();
            foreach (var kv in _filtresColonnes) anciens[kv.Key] = kv.Value.Text;

            foreach (var tb in _filtresColonnes.Values) { tableModelesDataGridView.Controls.Remove(tb); tb.Dispose(); }
            _filtresColonnes.Clear();

            foreach (DataGridViewColumn col in tableModelesDataGridView.Columns)
            {
                if (EstColonneAction(col)) continue;

                var tb = new Guna2TextBox
                {
                    Height = 28,
                    BorderRadius = 4,
                    BorderColor = Color.FromArgb(203, 213, 225),
                    Font = new Font("Segoe UI", 8.5F),
                    PlaceholderText = "Filtrer…",
                    Text = anciens.TryGetValue(col.Name, out var t) ? t : "",
                    Visible = false
                };
                tb.TextChanged += (s, e) => AppliquerFiltre();

                _filtresColonnes[col.Name] = tb;
                tableModelesDataGridView.Controls.Add(tb);
            }
        }

        /// <summary>Place chaque champ dans l'en-tête, juste sous le titre de sa colonne.</summary>
        private void PositionnerFiltresColonnes()
        {
            if (tableModelesDataGridView == null) return;

            foreach (var kv in _filtresColonnes)
            {
                var tb = kv.Value;
                if (!tableModelesDataGridView.Columns.Contains(kv.Key)) { tb.Visible = false; continue; }

                var col = tableModelesDataGridView.Columns[kv.Key];
                if (!col.Visible) { tb.Visible = false; continue; }

                Rectangle r = tableModelesDataGridView.GetColumnDisplayRectangle(col.Index, false);
                if (r.Width < 20) { tb.Visible = false; continue; }   // colonne hors écran

                tb.SetBounds(r.Left + 2, HauteurEntete - HauteurChampFiltre - 4, r.Width - 5, HauteurChampFiltre);
                tb.Visible = true;
            }
        }

        /// <summary>Texte décrivant les filtres actifs (utilisé dans l'impression).</summary>
        private string DescriptionFiltres()
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(filtreTextBox.Text))
            {
                string champ = ColonneFiltreSelectionnee().Length > 0 ? listeFiltrageComboBox.Text : "tous les champs";
                parts.Add($"{champ} contient « {filtreTextBox.Text.Trim()} »");
            }
            foreach (var kv in _filtresColonnes)
                if (!string.IsNullOrWhiteSpace(kv.Value.Text))
                    parts.Add($"{kv.Key} contient « {kv.Value.Text.Trim()} »");
            return string.Join(" ; ", parts);
        }

        /// <summary>
        /// Combine : recherche globale (barre du haut + liste de filtrage) ET filtres de chaque colonne.
        /// Sans accents, sans casse, tous les mots doivent être présents.
        /// </summary>
        /// 

        private void BtnViderFiltres_Click(object? sender, EventArgs e)
        {
            _effacementEnCours = true;   // évite de refiltrer à chaque champ vidé
            try
            {
                foreach (var tb in _filtresColonnes.Values) tb.Clear();
            }
            finally
            {
                _effacementEnCours = false;
            }
            AppliquerFiltre();           // un seul recalcul à la fin
        }

        private void AppliquerFiltre()
        {
            if (_effacementEnCours) return;
            if (tableModelesDataGridView.DataSource is not DataTable dt) return;

            DataView vue = dt.DefaultView;
            string[] mots = Normaliser(filtreTextBox.Text.Trim()).Split(' ', StringSplitOptions.RemoveEmptyEntries);

            var parColonne = new List<(string Col, string[] Mots)>();
            foreach (var kv in _filtresColonnes)
            {
                string[] m = Normaliser(kv.Value.Text.Trim()).Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (m.Length > 0 && dt.Columns.Contains(kv.Key)) parColonne.Add((kv.Key, m));
            }

            if (mots.Length == 0 && parColonne.Count == 0)
            {
                vue.RowFilter = "";
            }
            else
            {
                // Colonnes de la recherche globale : celle choisie, ou toutes les colonnes VISIBLES
                string colonne = ColonneFiltreSelectionnee();
                var cibles = new List<string>();
                if (colonne.Length > 0)
                {
                    if (dt.Columns.Contains(colonne)) cibles.Add(colonne);
                }
                else
                {
                    foreach (DataColumn col in dt.Columns)
                    {
                        var dgvCol = tableModelesDataGridView.Columns.Contains(col.ColumnName)
                            ? tableModelesDataGridView.Columns[col.ColumnName] : null;
                        if (dgvCol == null || dgvCol.Visible) cibles.Add(col.ColumnName);
                    }
                }

                // Filtrage fait en C# : les caractères [ ] * % ' ne posent aucun problème.
                var ids = new List<long>();
                foreach (DataRow r in dt.Rows)
                {
                    bool ok = true;

                    if (mots.Length > 0)
                    {
                        var sb = new StringBuilder();
                        foreach (string c in cibles)
                            sb.Append(r[c] == DBNull.Value ? "" : Convert.ToString(r[c], CultureInfo.CurrentCulture)).Append(" | ");
                        string texte = Normaliser(sb.ToString());
                        ok = mots.All(m => texte.Contains(m));
                    }

                    if (ok)
                    {
                        foreach (var (col, m) in parColonne)
                        {
                            string v = Normaliser(r[col] == DBNull.Value ? "" : Convert.ToString(r[col], CultureInfo.CurrentCulture));
                            if (!m.All(x => v.Contains(x))) { ok = false; break; }
                        }
                    }

                    if (ok) ids.Add(Convert.ToInt64(r["ID"]));
                }

                vue.RowFilter = ids.Count == 0 ? "1 = 0" : $"[ID] IN ({string.Join(",", ids)})";
            }

            lblCompteur.Text = (vue.Count == dt.Rows.Count)
                ? $"{dt.Rows.Count} article(s)"
                : $"{vue.Count} affiché(s) sur {dt.Rows.Count}";
        }

        private void BtnNouveauModele_Click(object? sender, EventArgs e)
        {
            using (var frm = new FrmAjouterModele())
            {
                if (frm.ShowDialog(this) == DialogResult.OK)
                {
                    DernierModeleModifieId = frm.ModeleIdResultat;
                    ChargerListe();
                }
            }
        }

        private void BtnChoisirColonnes_Click(object? sender, EventArgs e)
        {
            var menu = new Guna2ContextMenuStrip();
            foreach (DataGridViewColumn col in tableModelesDataGridView.Columns)
            {
                var item = new ToolStripMenuItem(col.HeaderText) { Checked = col.Visible, CheckOnClick = true };
                item.Click += (s, args) =>
                {
                    col.Visible = item.Checked;
                    if (!col.Visible && _filtresColonnes.TryGetValue(col.Name, out var tb)) tb.Clear();
                    PositionnerFiltresColonnes();
                    AppliquerFiltre();
                };
                menu.Items.Add(item);
            }
            menu.Show(btnChoisirColonnes, new Point(0, btnChoisirColonnes.Height));
        }

        // ───────────── IMPRESSION (style « ancien » : en-tête officiel + groupes) ─────────────

        private static readonly string[] ColonnesGroupables = { "Catégorie", "Marque", "Emplacement" };

        private const string CssImpression =
            "body{font-family:Times New Roman,Arial, sans-serif; margin:25px; color:#000; direction:ltr;}" +
            ".header-officiel{font-family:Times New Roman,Arial, serif; margin-bottom:25px;}" +
            ".republique{font-size:24px; font-weight:bold; text-align:center; text-decoration:underline; margin-bottom:12px;}" +
            ".ministere{font-size:16px; font-weight:bold; text-align:right; direction:rtl; line-height:1.6;}" +
            ".divider{border-bottom:1.5px solid #000; margin:15px 0 20px 0;}" +
            "h1{font-size:18px; text-align:center; color:#1a237e;}" +
            "h2{font-size:13px; color:#1a237e; margin-top:20px;}" +
            "table{border-collapse:collapse; width:100%; margin-bottom:15px;}" +
            "thead{display:table-header-group;} tr{page-break-inside:avoid;}" +
            "th,td{border:1px solid #777; padding:5px 8px; font-size:11px; text-align:left; word-wrap:break-word; overflow-wrap:anywhere;}" +
            "th{background:#f0f2f5;}" +
            ".info{text-align:center; font-size:11px;}" +
            ".fiche th{width:30%;}" +
            "@media print{.no-print{display:none;}}";

        private static void AjouterEnteteOfficiel(StringBuilder html)
        {
            html.Append("<div class='header-officiel'>");
            html.Append("  <div class='republique'>الجمهورية الجزائرية الديمقراطية الشعبية</div>");
            html.Append("  <div class='ministere'>");
            html.Append("    <div>وزارة الداخليـــــة و الجماعات المحلية .</div>");
            html.Append("    <div>ولايــــة غليزان </div>");
            html.Append("    <div>مديرية المواصلات السلكية و اللاسلكية الوطنية</div>");
            html.Append("    <div>مصلحة الادارة و الامداد / مكتب الوسائل العامة و المخزن.</div>");
            html.Append("  </div></div>");   // ferme .ministere ET .header-officiel (sans la ligne de séparation)
        }

        private static void OuvrirHtml(StringBuilder html, string nomFichier)
        {
            string tempFile = Path.Combine(Path.GetTempPath(), nomFichier);
            File.WriteAllText(tempFile, html.ToString(), new UTF8Encoding(true));
            Process.Start(new ProcessStartInfo(tempFile) { UseShellExecute = true });
        }

        /// <summary>Colonne de regroupement : celle du filtre si elle s'y prête, sinon « Catégorie ».</summary>
        private string ColonneGroupement()
        {
            string c = ColonneFiltreSelectionnee();
            return ColonnesGroupables.Contains(c) ? c : "Catégorie";
        }

        private void BtnImprimer_Click(object? sender, EventArgs e)
        {
            if (tableModelesDataGridView.DataSource is not DataTable dt) return;
            DataView vue = dt.DefaultView;   // contient déjà les filtres en cours

            if (vue.Count == 0)
            {
                MessageBox.Show("Aucune donnée à imprimer.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string colGroupe = ColonneGroupement();
            if (!dt.Columns.Contains(colGroupe)) return;

            // Regroupement (l'ordre de tri du tableau est conservé à l'intérieur de chaque groupe)
            var groupes = new SortedDictionary<string, List<DataRowView>>(StringComparer.CurrentCultureIgnoreCase);
            foreach (DataRowView row in vue)
            {
                string cle = row[colGroupe]?.ToString() ?? "";
                if (string.IsNullOrWhiteSpace(cle) || cle == "—") cle = "(Non défini)";
                if (!groupes.ContainsKey(cle)) groupes[cle] = new List<DataRowView>();
                groupes[cle].Add(row);
            }

            var colsAffichees = new List<DataGridViewColumn>();
            foreach (DataGridViewColumn col in tableModelesDataGridView.Columns)
                if (col.Visible && !EstColonneAction(col)) colsAffichees.Add(col);

            // Police et marges adaptées au nombre de colonnes pour que TOUT tienne sur la largeur de la page
            int nb = colsAffichees.Count;
            int taille = nb > 12 ? 8 : nb > 9 ? 9 : nb > 6 ? 10 : 11;
            string cssListe = "@page{size:A4 landscape; margin:10mm;}"
                            + $"th,td{{font-size:{taille}px; padding:4px 5px;}}"
                            + "@media print{body{margin:0;}}";

            var html = new StringBuilder();
            html.Append("<html dir='ltr'><head><meta charset='utf-8'><style>").Append(CssImpression).Append(cssListe).Append("</style></head><body>");
            AjouterEnteteOfficiel(html);

            html.Append($"<h1>Liste des articles — groupée par {WebUtility.HtmlEncode(colGroupe)}</h1>");
            string descFiltres = DescriptionFiltres();
            if (descFiltres.Length > 0)
                html.Append($"<div class='info'>Filtre : {WebUtility.HtmlEncode(descFiltres)} — {vue.Count} sur {dt.Rows.Count} article(s)</div>");
            html.Append($"<div class='info'>Généré le {DateTime.Now:dd/MM/yyyy HH:mm} <span class='no-print'>— (Ctrl+P pour imprimer)</span></div>");

            foreach (var groupe in groupes)
            {
                html.Append($"<h2>{WebUtility.HtmlEncode(groupe.Key)} ({groupe.Value.Count} article(s))</h2><table><thead><tr>");
                foreach (var col in colsAffichees)
                    html.Append($"<th>{WebUtility.HtmlEncode(col.HeaderText)}</th>");
                html.Append("</tr></thead><tbody>");

                foreach (var row in groupe.Value)
                {
                    html.Append("<tr>");
                    foreach (var col in colsAffichees)
                    {
                        string field = string.IsNullOrEmpty(col.DataPropertyName) ? col.Name : col.DataPropertyName;
                        string val = dt.Columns.Contains(field) ? row[field]?.ToString() ?? "" : "";
                        html.Append($"<td>{WebUtility.HtmlEncode(val)}</td>");
                    }
                    html.Append("</tr>");
                }
                html.Append("</tbody></table>");
            }
            html.Append("</body></html>");

            OuvrirHtml(html, $"rapport_articles_{DateTime.Now:yyyyMMdd_HHmmss}.html");
        }

        private Image? ChargerImageLocale(string fileName)
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "image", fileName);
            if (!File.Exists(path)) path = Path.Combine("image", fileName);
            return File.Exists(path) ? Image.FromFile(path) : null;
        }

        private void Dgv_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0) return;
            Point mousePos = tableModelesDataGridView.PointToClient(Cursor.Position);
            bool isHovered = e.CellBounds.Contains(mousePos);
            bool isClicked = isHovered && (Control.MouseButtons == MouseButtons.Left);

            if (e.ColumnIndex == tableModelesDataGridView.Columns["colModifier"]?.Index)
                DessinerBouton(e, isHovered, isClicked, Color.FromArgb(240, 253, 244), Color.FromArgb(220, 252, 231), Color.FromArgb(187, 247, 208), Color.FromArgb(134, 239, 172), "pencil_icon.png");
            else if (e.ColumnIndex == tableModelesDataGridView.Columns["colSupprimer"]?.Index)
                DessinerBouton(e, isHovered, isClicked, Color.FromArgb(254, 242, 242), Color.FromArgb(254, 226, 226), Color.FromArgb(254, 202, 202), Color.FromArgb(252, 165, 165), "delet_icon.png");
            else if (e.ColumnIndex == tableModelesDataGridView.Columns["colImprimer"]?.Index)
                DessinerBouton(e, isHovered, isClicked, Color.FromArgb(239, 246, 255), Color.FromArgb(219, 234, 254), Color.FromArgb(191, 219, 254), Color.FromArgb(147, 197, 253), "imprimerbleu.png");
        }

        private static void DessinerBouton(DataGridViewCellPaintingEventArgs e, bool isHovered, bool isClicked, Color bg, Color bgHover, Color bgClick, Color border, string iconFile)
        {
            if (e.Graphics == null) return;

            e.PaintBackground(e.CellBounds, true);
            Color cur = isClicked ? bgClick : (isHovered ? bgHover : bg);
            Rectangle rect = new Rectangle(e.CellBounds.Left + 4, e.CellBounds.Top + 4, e.CellBounds.Width - 8, e.CellBounds.Height - 8);
            using (var brush = new SolidBrush(cur)) e.Graphics.FillRectangle(brush, rect);
            using (var pen = new Pen(border)) e.Graphics.DrawRectangle(pen, rect);

            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "image", iconFile);
            if (!File.Exists(path)) path = Path.Combine("image", iconFile);
            if (File.Exists(path))
            {
                using (Image img = Image.FromFile(path))
                {
                    int size = 18;
                    e.Graphics.DrawImage(img, new Rectangle(rect.Left + (rect.Width - size) / 2, rect.Top + (rect.Height - size) / 2, size, size));
                }
            }
            e.Handled = true;
        }

        private void Dgv_CellClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            string colName = tableModelesDataGridView.Columns[e.ColumnIndex].Name;
            int id = Convert.ToInt32(tableModelesDataGridView.Rows[e.RowIndex].Cells["ID"].Value);

            if (colName == "colModifier")
            {
                using (var frm = new FrmAjouterModele(id))
                {
                    if (frm.ShowDialog(this) == DialogResult.OK)
                    {
                        DernierModeleModifieId = id;
                        ChargerListe();
                        //_mainForm?.ChargerEquipements();
                    }
                }
            }
            else if (colName == "colSupprimer")
            {
                int Sorties = Convert.ToInt32(tableModelesDataGridView.Rows[e.RowIndex].Cells["Sorties"].Value);
                int Entrées = Convert.ToInt32(tableModelesDataGridView.Rows[e.RowIndex].Cells["Entrées"].Value);
                string designation = tableModelesDataGridView.Rows[e.RowIndex].Cells["Désignation"].Value?.ToString() ?? "";
                string catie = tableModelesDataGridView.Rows[e.RowIndex].Cells["Catégorie"].Value?.ToString() ?? "";
                string marque = tableModelesDataGridView.Rows[e.RowIndex].Cells["Marque"].Value?.ToString() ?? "";
                if (Sorties > 0 || Entrées > 0)
                {
                    MessageBox.Show(
                        $"Impossible de supprimer l'article n°{id} [{catie} {marque} {designation}] : {Sorties + Entrées}    BON de mouvement(s) utilisent encore ce Article.\n\n" +
                        "Modifiez ou supprimez d'abord ces articles dans les bons de mouvements.",
                        "Suppression refusée", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var confirm = MessageBox.Show($"Supprimer définitivement l'article n°{id} [{catie} {marque} {designation}] ?",
                    "Confirmer la suppression", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (confirm != DialogResult.Yes) return;

                try
                {
                    DatabaseHelper.ExecuteNonQuery("DELETE FROM Modele WHERE id=@id", new SqliteParameter("@id", id));
                    ChargerListe();
                }
                catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
                {
                    MessageBox.Show("Suppression refusée par la base de données : ce article est encore utilisé.",
                        "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else if (colName == "colImprimer")
            {
                ImprimerFicheIndividuelle(tableModelesDataGridView.Rows[e.RowIndex]);
            }
        }

        private void ImprimerFicheIndividuelle(DataGridViewRow row)
        {
            var html = new StringBuilder();
            html.Append("<html dir='ltr'><head><meta charset='utf-8'><style>").Append(CssImpression).Append("</style></head><body>");
            AjouterEnteteOfficiel(html);
            html.Append("<h1>FICHE D'ARTICLE</h1>");
            html.Append($"<div class='info'>Généré le {DateTime.Now:dd/MM/yyyy HH:mm} <span class='no-print'>— (Ctrl+P pour imprimer)</span></div>");
            html.Append("<table class='fiche' style='margin-top:20px'>");

            foreach (DataGridViewColumn col in tableModelesDataGridView.Columns)
            {
                if (col.Visible && !EstColonneAction(col))
                {
                    string val = row.Cells[col.Index].Value?.ToString() ?? "";
                    html.Append($"<tr><th>{WebUtility.HtmlEncode(col.HeaderText)}</th><td>{WebUtility.HtmlEncode(val)}</td></tr>");
                }
            }
            html.Append("</table></body></html>");

            OuvrirHtml(html, $"fiche_article_{row.Cells["ID"].Value}.html");
        }
    }
}