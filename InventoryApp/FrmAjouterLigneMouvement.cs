#nullable enable
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Drawing;
using System.Windows.Forms;
using Guna.UI2.WinForms;
using InventoryApp.Data;
using Microsoft.Data.Sqlite;

namespace InventoryApp
{
    // Une ligne de mouvement porte désormais sur un Modèle + une quantité
    // (entrée "+" ou sortie "-"), et non plus sur un équipement physique individuel,
    // car la table Equipement n'existe plus dans la base migrée : le stock est
    // suivi de façon globale via Modele.quantite. modele
    public class LigneMouvementTemp
    {
        public int ModeleId { get; set; }
        public string Affichage { get; set; } = "";
        public int Quantite { get; set; } = 1;
        public string Etat { get; set; } = "Bon";
        public bool EstSortie { get; set; } = true;
        public string? Observation { get; set; }
    }

    public class FrmAjouterLigneMouvement : Form
    {
        private readonly Color _primaryBlue = Color.FromArgb(37, 99, 235);
        private readonly Color _darkNavy = Color.FromArgb(24, 30, 54);
        private readonly Color _lightGray = Color.FromArgb(240, 242, 245);

        public LigneMouvementTemp? LigneResultat { get; private set; } = null;

        private readonly LigneMouvementTemp? _ligneAModifier;
        private bool EnModeEdition => _ligneAModifier != null;

        // Quantité réellement disponible pour le modèle sélectionné :
        //   stock en base
        //   + annulation des lignes DÉJÀ ENREGISTRÉES de ce mouvement (mode édition du mouvement)
        //   + effet net des AUTRES lignes de la liste en cours (non encore enregistrées).
        private long _quantiteDisponible = 0;

        // Les autres lignes de la liste du mouvement (sans la ligne en cours de modification)
        private readonly List<LigneMouvementTemp> _autresLignes;
        // Id du mouvement en cours de modification (null = nouveau mouvement)
        private readonly long? _mouvementId;

        private bool _initialisation = false;   // évite les rechargements parasites au chargement
        private bool _premiereCharge = true;    // restauration de la ligne d'origine une seule fois
        private bool _chargementListe = false;  // évite les recalculs pendant le remplissage du combo

        private string MvtSql => _mouvementId.HasValue
            ? _mouvementId.Value.ToString(CultureInfo.InvariantCulture)
            : "NULL";

        // Stock "neutralisé" du mouvement : Modele.quantite (déjà modifiée par les triggers de la base
        // pour les lignes enregistrées) dont on retire l'effet des lignes enregistrées de CE mouvement.
        // Sortie enregistrée => on rend la quantité ; Entrée enregistrée => on la retire.
        private string SqlStockSansCeMouvement =>
            $@"(m.quantite + COALESCE((SELECT SUM(CASE WHEN lm.est_sortie = 1 THEN lm.quantite ELSE -lm.quantite END)
                                       FROM Ligne_mouvement lm
                                       WHERE lm.mouvement_id = {MvtSql} AND lm.modele_id = m.id), 0))";

        private long EffetNetAutresLignes(int modeleId) =>
            _autresLignes.Where(l => l.ModeleId == modeleId)
                         .Sum(l => l.EstSortie ? -(long)l.Quantite : (long)l.Quantite);

        private Guna2ComboBox cmbModele = null!;
        private NumericUpDown numQuantite = null!;
        private Guna2ComboBox cmbEtat = null!;
        private Guna2ToggleSwitch tglEstSortie = null!;
        private Label lblToggleEtat = null!;
        private Guna2TextBox txtObservation = null!;
        private Guna2Button btnAjouter = null!;
        private Guna2Button btnAnnuler = null!;
        private Guna2Button btnNouveauModele = null!;
        // Modèle qu'on vient de créer : à afficher et sélectionner même s'il a un stock à 0 en mode Sortie
        private long? _modeleIdForce = null;
        private long? _selectionForcee = null;
        private Label lblStatutActuel = null!;

        /// <param name="ligneAModifier">Ligne à modifier (null = ajout)</param>
        /// <param name="autresLignes">Les AUTRES lignes de la liste courante (sans la ligne modifiée)</param>
        /// <param name="mouvementId">Id du mouvement en édition (null = nouveau mouvement)</param>
        public FrmAjouterLigneMouvement(LigneMouvementTemp? ligneAModifier = null,
                                        IEnumerable<LigneMouvementTemp>? autresLignes = null,
                                        long? mouvementId = null)
        {
            _ligneAModifier = ligneAModifier;
            _autresLignes = autresLignes?.ToList() ?? new List<LigneMouvementTemp>();
            _mouvementId = mouvementId;

            Text = EnModeEdition ? "Modifier la ligne de mouvement" : "Ajouter une ligne de mouvement";
            Size = new Size(460, 460);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.None;
            BackColor = Color.White;

            ConstruireControles();
            Load += (s, e) =>
            {
                _initialisation = true;
                if (EnModeEdition && _ligneAModifier != null)
                {
                    tglEstSortie.Checked = _ligneAModifier.EstSortie;
                    cmbEtat.SelectedItem = _ligneAModifier.Etat;
                    txtObservation.Text = _ligneAModifier.Observation ?? "";
                }
                _initialisation = false;
                ChargerModeles();
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

            var lblTitre = new Label
            {
                Text = EnModeEdition ? "MODIFIER LIGNE DE MOUVEMENT" : "AJOUTER LIGNE DE MOUVEMENT",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            panelHeader.Controls.Add(lblTitre);
            Controls.Add(panelHeader);

            int y = 70;
            const int marge = 25;
            const int largeur = 390;

            Label MakeLabel(string texte)
            {
                var l = new Label
                {
                    Text = texte,
                    Left = marge,
                    Top = y,
                    Width = largeur,
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                    ForeColor = _darkNavy
                };
                Controls.Add(l);
                y += 22;
                return l;
            }

            // 1. Toggle Switch (Sortie "-" / Entrée "+")
            lblToggleEtat = new Label
            {
                Left = marge,
                Top = y + 4,
                Width = 280,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = _darkNavy,
                Text = "Sortie (-) : la quantité quitte le stock"
            };
            tglEstSortie = new Guna2ToggleSwitch
            {
                Left = marge + 290,
                Top = y,
                Checked = true,
                CheckedState = { FillColor = _primaryBlue }
            };

            tglEstSortie.CheckedChanged += (s, e) =>
            {
                lblToggleEtat.Text = tglEstSortie.Checked
                    ? "Sortie (-) : la quantité quitte le stock"
                    : "Entrée (+) : la quantité rejoint le stock";
                // La liste des modèles proposés et la limite de quantité dépendent du sens du mouvement
                if (_initialisation) return;
                ChargerModeles();
            };

            Controls.Add(lblToggleEtat);
            Controls.Add(tglEstSortie);
            y += 42;

            // 2. Champ Modèle/article
            MakeLabel("Article *");
            cmbModele = new Guna2ComboBox { Left = marge, Top = y, Width = largeur - 110, Height = 36, BorderRadius = 6, DropDownStyle = ComboBoxStyle.DropDownList };
            btnNouveauModele = new Guna2Button
            {
                Text = "+ Nouveau",
                Left = marge + largeur - 100,
                Top = y,
                Width = 100,
                Height = 36,
                BorderRadius = 6,
                FillColor = _primaryBlue,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnNouveauModele.Click += BtnNouveauModele_Click;
            Controls.Add(btnNouveauModele);
            cmbModele.SelectedIndexChanged += (s, e) => { if (!_chargementListe) AfficherInfosModele(); };
            Controls.Add(cmbModele); y += 42;

            lblStatutActuel = new Label { Left = marge, Top = y, Width = largeur, Font = new Font("Segoe UI", 8.5F, FontStyle.Italic), ForeColor = _primaryBlue, Text = "" };
            Controls.Add(lblStatutActuel); y += 26;

            // 3. Champ Quantité
            MakeLabel("Quantité *");
            numQuantite = new NumericUpDown
            {
                Left = marge,
                Top = y,
                Width = 120,
                Height = 36,
                Minimum = 1,
                Maximum = 999999,
                Value = 1,
                Font = new Font("Segoe UI", 10F)
            };
            numQuantite.ValueChanged += (s, e) => MettreAJourStatut();
            Controls.Add(numQuantite); y += 45;

            // 4. État à ce moment
            MakeLabel("État à ce moment");
            cmbEtat = new Guna2ComboBox { Left = marge, Top = y, Width = largeur, Height = 36, BorderRadius = 6, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbEtat.Items.AddRange(new object[] { "Neuf", "Bon", "Usé", "Endommagé", "Hors service" });
            cmbEtat.SelectedIndex = 1;
            Controls.Add(cmbEtat); y += 45;

            // 5. Observation
            MakeLabel("Observation (optionnel)");
            txtObservation = new Guna2TextBox { Left = marge, Top = y, Width = largeur, Height = 36, BorderRadius = 6 };
            Controls.Add(txtObservation); y += 50;

            // Boutons d'action
            btnAjouter = new Guna2Button
            {
                Text = EnModeEdition ? "Enregistrer" : "Ajouter à la liste",
                Left = marge + 190,
                Top = y,
                Width = 200,
                Height = 40,
                BorderRadius = 6,
                FillColor = _primaryBlue,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };

            btnAnnuler = new Guna2Button
            {
                Text = "Annuler",
                Left = marge,
                Top = y,
                Width = 175,
                Height = 40,
                BorderRadius = 6,
                FillColor = _lightGray,
                ForeColor = _darkNavy,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };

            btnAjouter.Click += BtnAjouter_Click;
            btnAnnuler.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            Controls.Add(btnAjouter);
            Controls.Add(btnAnnuler);
        }

        private void ChargerModeles()
        {
            bool estSortie = tglEstSortie.Checked;

            // Modèle à resélectionner après rechargement : la ligne d'origine au premier chargement,
            // ensuite le choix courant de l'utilisateur (on ne l'écrase pas quand il bascule Entrée/Sortie).
            long? idAConserver = null;
            bool restaurerLigne = _premiereCharge && EnModeEdition;
            if (restaurerLigne)
                idAConserver = _ligneAModifier!.ModeleId;
            else if (cmbModele.SelectedValue != null && cmbModele.SelectedValue != DBNull.Value)
                idAConserver = Convert.ToInt64(cmbModele.SelectedValue);
            if (_selectionForcee.HasValue)
            {
                idAConserver = _selectionForcee.Value;
                _selectionForcee = null;
            }
            _premiereCharge = false;

            // En Sortie ("-") : modèles avec du stock, + ceux déjà impliqués dans ce mouvement
            // (lignes de la liste, ligne modifiée, lignes déjà enregistrées) pour qu'ils ne disparaissent pas.
            // En Entrée ("+") : tous les modèles.
            string filtre = "";
            if (estSortie)
            {
                var ids = _autresLignes.Select(l => l.ModeleId).ToList();
                if (EnModeEdition) ids.Add(_ligneAModifier!.ModeleId);
                if (_modeleIdForce.HasValue) ids.Add((int)_modeleIdForce.Value);
                string liste = ids.Count > 0 ? string.Join(",", ids.Distinct()) : "-1";
                filtre = $@"WHERE (m.quantite > 0
                                   OR m.id IN ({liste})
                                   OR EXISTS (SELECT 1 FROM Ligne_mouvement x
                                              WHERE x.mouvement_id = {MvtSql} AND x.modele_id = m.id))";
            }

            // UNE seule requête : liste + stock neutralisé du mouvement
            string sql = $@"
                SELECT m.id,
                       TRIM(COALESCE(c.designation,'') || ' ' || COALESCE(mq.designation,'') || ' ' || m.designation || ' ' || COALESCE(m.reference,'')) AS affichage,
                       {SqlStockSansCeMouvement} AS quantite
                FROM Modele m
                LEFT JOIN Categorie c ON m.categorie_id = c.id
                LEFT JOIN Marque mq ON m.marque_id = mq.id
                {filtre}
                ORDER BY m.designation";

            var t = DatabaseHelper.ExecuteQuery(sql);

            _chargementListe = true;
            try
            {
                cmbModele.DataSource = null;
                cmbModele.DisplayMember = "affichage";
                cmbModele.ValueMember = "id";
                cmbModele.DataSource = t;

                if (t.Rows.Count == 0)
                {
                    lblStatutActuel.Text = estSortie ? "Aucun modèle avec du stock disponible." : "Aucun modèle disponible.";
                    _quantiteDisponible = 0;
                    return;
                }

                if (idAConserver.HasValue)
                    cmbModele.SelectedValue = idAConserver.Value;

                // Restauration de la quantité d'origine : Maximum d'abord, Value ensuite
                // (sinon NumericUpDown lève ArgumentOutOfRangeException si Value > Maximum).
                if (restaurerLigne && cmbModele.SelectedValue != null
                    && Convert.ToInt64(cmbModele.SelectedValue) == _ligneAModifier!.ModeleId)
                {
                    numQuantite.Maximum = 999999;
                    numQuantite.Value = Math.Min(numQuantite.Maximum, Math.Max(numQuantite.Minimum, _ligneAModifier.Quantite));
                }
            }
            finally
            {
                _chargementListe = false;
            }

            AfficherInfosModele();
        }

        private void AfficherInfosModele()
        {
            if (cmbModele.SelectedItem is DataRowView rowView)
            {
                int idSelectionne = Convert.ToInt32(rowView["id"]);
                long stockBase = rowView["quantite"] != DBNull.Value ? Convert.ToInt64(rowView["quantite"]) : 0;

                // Le stock proposé tient compte des lignes déjà enregistrées de ce mouvement (annulées)
                // ET des autres lignes de la liste en cours (pas encore en base).
                _quantiteDisponible = stockBase + EffetNetAutresLignes(idSelectionne);

                // On NE bride PAS la quantité saisie : l'utilisateur doit voir un message explicite
                // s'il demande plus que le disponible (au lieu d'une correction silencieuse).
                numQuantite.Maximum = 999999;
                MettreAJourStatut();
            }
            else
            {
                _quantiteDisponible = 0;
                lblStatutActuel.Text = "";
            }
        }

        // Message d'information sous la liste : disponible, et avertissement si la sortie demandée le dépasse.
        private void MettreAJourStatut()
        {
            if (!(cmbModele.SelectedItem is DataRowView))
                return;

            long dispo = _quantiteDisponible;
            long demande = (long)numQuantite.Value;

            if (!tglEstSortie.Checked)
            {
                lblStatutActuel.Text = $"Quantité disponible en stock : {dispo}";
                lblStatutActuel.ForeColor = _primaryBlue;
            }
            else if (dispo <= 0)
            {
                lblStatutActuel.Text = "Aucun stock disponible : sortie impossible.";
                lblStatutActuel.ForeColor = Color.Red;
            }
            else if (demande > dispo)
            {
                lblStatutActuel.Text = $"Disponible : {dispo} — demandé : {demande}. Il manque {demande - dispo}.";
                lblStatutActuel.ForeColor = Color.Red;
            }
            else
            {
                lblStatutActuel.Text = $"Quantité disponible en stock : {dispo}";
                lblStatutActuel.ForeColor = _primaryBlue;
            }
        }

        private void BtnNouveauModele_Click(object? sender, EventArgs e)
        {
            using (var frm = new FrmAjouterModele())
            {
                if (frm.ShowDialog(this) == DialogResult.OK)
                {
                    // ModeleIdResultat peut être int, long ou nullable : on passe par object pour tous les cas
                    object? brut = frm.ModeleIdResultat;
                    if (brut != null && brut != DBNull.Value)
                        _modeleIdForce = Convert.ToInt64(brut);

                    // Recharge la liste et sélectionne le nouveau modèle (idAConserver = choix courant)
                    if (_modeleIdForce.HasValue)
                        SelectionnerApresChargement(_modeleIdForce.Value);
                    else
                        ChargerModeles();
                }
            }
        }

        private void SelectionnerApresChargement(long modeleId)
        {
            _selectionForcee = modeleId;
            ChargerModeles();
        }

        private void BtnAjouter_Click(object? sender, EventArgs e)
        {
            if (cmbModele.SelectedValue == null || cmbModele.SelectedValue == DBNull.Value)
            {
                MessageBox.Show("Veuillez choisir un modèle.", "Champ manquant",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int modeleId = Convert.ToInt32(cmbModele.SelectedValue);
            int quantiteDemandee = (int)numQuantite.Value;

            if (quantiteDemandee <= 0)
            {
                MessageBox.Show("La quantité doit être supérieure à 0.", "Quantité invalide",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            bool estSortie = tglEstSortie.Checked;

            // Contrôle rapide (confort utilisateur) : la quantité en base est relue au moment de la validation,
            // puis on applique le même calcul que l'affichage (stock neutralisé + autres lignes de la liste).
            // Le contrôle définitif, cumulé par modèle, est refait dans FrmAjouterMouvement au moment de l'enregistrement.
            if (estSortie)
            {
                var t = DatabaseHelper.ExecuteQuery(
                    $"SELECT {SqlStockSansCeMouvement} AS stock FROM Modele m WHERE m.id = {modeleId}");

                if (t.Rows.Count == 0)
                {
                    MessageBox.Show("Ce modèle n'existe plus dans la base.", "Modèle introuvable",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                long netAutres = EffetNetAutresLignes(modeleId);
                long disponible = Convert.ToInt64(t.Rows[0]["stock"]) + netAutres;

                if (quantiteDemandee > disponible)
                {
                    string detail = netAutres < 0
                        ? $"\n({-netAutres} unité(s) sont déjà réservées par d'autres lignes de ce mouvement.)"
                        : "";
                    MessageBox.Show(
                        $"Quantité demandée ({quantiteDemandee}) supérieure au stock disponible ({Math.Max(0, disponible)}) pour ce modèle." +
                        detail + "\nImpossible d'effectuer cette sortie.",
                        "Quantité incompatible avec le stock", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            LigneResultat = new LigneMouvementTemp
            {
                ModeleId = modeleId,
                Affichage = $"{cmbModele.Text}  x {quantiteDemandee}",
                Quantite = quantiteDemandee,
                Etat = cmbEtat.SelectedItem?.ToString() ?? "Bon",
                EstSortie = estSortie,
                Observation = string.IsNullOrWhiteSpace(txtObservation.Text) ? null : txtObservation.Text.Trim()
            };

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}