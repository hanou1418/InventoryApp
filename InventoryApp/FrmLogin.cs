#nullable enable
using System;
using System.Drawing;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Guna.UI2.WinForms;
using InventoryApp.Data;
using Microsoft.Data.Sqlite;

namespace InventoryApp
{
    public class FrmLogin : Form
    {
        private const int MAX_TENTATIVES = 5;
        private const int BLOCAGE_SECONDES = 60;

        private int _tentatives = 0;
        private System.Windows.Forms.Timer? _timerBlocage;
        private int _secondesRestantes = 0;

        private Guna2BorderlessForm borderlessForm = null!;

        // Conteneurs principaux
        private Guna2CustomGradientPanel pnlHeader = null!;
        private Guna2Panel pnlContent = null!;

        // Éléments du Header Officiel
        private Guna2CirclePictureBox picMinistere = null!;
        private Guna2CirclePictureBox picDirection = null!;

        private Label lblRepublique = null!;
        private Label lblMinistere = null!;
        private Label lblDirection = null!;
        private Label lblWilaya = null!;

        // Éléments du Formulaire de Connexion
        private Label lblTitreApp = null!;
        private Label lblSousTitre = null!;

        private Label lblLogin = null!;
        private Guna2TextBox txtLogin = null!;
        private Label lblMdp = null!;
        private Guna2TextBox txtMdp = null!;
        private Guna2CheckBox chkAfficherMdp = null!;

        private Guna2Button btnConnexion = null!;
        private Guna2Button btnQuitter = null!;
        private Label lblErreur = null!;
        private Label lblBlocage = null!;

        public FrmLogin()
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.White;
            Width = 900;
            Height = 620;

            ConstruireControles();
        }

        private Image? ChargerImageDirecte(string nomFichier)
        {
            try
            {
                string dossierProjet = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\.."));
                string cheminProjet = Path.Combine(dossierProjet, "image", nomFichier);
                if (File.Exists(cheminProjet)) return Image.FromFile(cheminProjet);

                string cheminBin = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "image", nomFichier);
                if (File.Exists(cheminBin)) return Image.FromFile(cheminBin);
            }
            catch { }

            return null;
        }

        private void ConstruireControles()
        {
            borderlessForm = new Guna2BorderlessForm
            {
                ContainerControl = this,
                BorderRadius = 16,
                DragForm = true,
                HasFormShadow = true
            };

            // =========================================================
            // PANNEAU SUPÉRIEUR (HEADER INSTITUTIONNEL HORIZONTAL)
            // =========================================================
            pnlHeader = new Guna2CustomGradientPanel
            {
                Dock = DockStyle.Top,
                Height = 180,
                FillColor = Color.Green,
                FillColor2 = Color.WhiteSmoke,
                FillColor3 = Color.FromArgb(0, 64, 0),
                FillColor4 = Color.White
            };

            // Logo Gauche : Ministère
            picMinistere = new Guna2CirclePictureBox
            {
                Size = new Size(90, 90),
                Left = 45,
                Top = 48,
                SizeMode = PictureBoxSizeMode.StretchImage,
                BackColor = Color.Transparent,
                Image = ChargerImageDirecte("imagelogodtn.jpg")
            };

            // Logo Droit : Direction
            picDirection = new Guna2CirclePictureBox
            {
                Size = new Size(90, 90),
                Left = 775,
                Top = 48,
                SizeMode = PictureBoxSizeMode.StretchImage,
                BackColor = Color.Transparent,
                Image = ChargerImageDirecte("MinistereLogo.jfif")
            };

            // 1. République (Titre principal)
            lblRepublique = new Label
            {
                Font = new Font("Times New Roman", 22F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter,
                Left = 100,
                Top = 25,
                Width = 700,
                Height = 40
            };
            string txtRepublique = "الجمهورية الجزائرية الديمقراطية الشعبية";

            lblRepublique.Paint += (s, e) =>
            {
                e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                TextFormatFlags flags = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak;

                // Ombre
                TextRenderer.DrawText(e.Graphics, txtRepublique, lblRepublique.Font,
                    new Rectangle(2, 2, lblRepublique.Width, lblRepublique.Height),
                    Color.FromArgb(150, 0, 0, 0), flags);

                // Texte Principal
                TextRenderer.DrawText(e.Graphics, txtRepublique, lblRepublique.Font,
                    new Rectangle(0, 0, lblRepublique.Width, lblRepublique.Height),
                    Color.White, flags);
            };

            // 2. Texte Ministère (Centré avec Shadow)
            lblMinistere = new Label
            {
                Font = new Font("Times New Roman", 16F, FontStyle.Bold),
                ForeColor = Color.WhiteSmoke,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter,
                Left = 135,
                Top = 70,
                Width = 630,
                Height = 30
            };
            string txtMinistere = "وزارة الداخلية والجماعات المحلية والنقل";

            lblMinistere.Paint += (s, e) =>
            {
                e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                TextFormatFlags flags = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak;

                // Ombre (décalage léger de 1px car la police est plus petite)
                TextRenderer.DrawText(e.Graphics, txtMinistere, lblMinistere.Font,
                    new Rectangle(1, 1, lblMinistere.Width, lblMinistere.Height),
                    Color.FromArgb(140, 0, 0, 0), flags);

                // Texte Principal
                TextRenderer.DrawText(e.Graphics, txtMinistere, lblMinistere.Font,
                    new Rectangle(0, 0, lblMinistere.Width, lblMinistere.Height),
                    Color.WhiteSmoke, flags);
            };

            // 3. Direction Générale (Centrée avec Shadow)
            lblDirection = new Label
            {
                Font = new Font("Times New Roman", 14F, FontStyle.Bold),
                ForeColor = Color.WhiteSmoke,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter,
                Left = 135,
                Top = 106,
                Width = 630,
                Height = 30
            };
            string txtDirection = "المديرية العامة للمواصلات السلكية واللاسلكية الوطنية";

            lblDirection.Paint += (s, e) =>
            {
                e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                TextFormatFlags flags = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak;

                // Ombre (décalage léger de 1px)
                TextRenderer.DrawText(e.Graphics, txtDirection, lblDirection.Font,
                    new Rectangle(1, 1, lblDirection.Width, lblDirection.Height),
                    Color.FromArgb(140, 0, 0, 0), flags);

                // Texte Principal
                TextRenderer.DrawText(e.Graphics, txtDirection, lblDirection.Font,
                    new Rectangle(0, 0, lblDirection.Width, lblDirection.Height),
                    Color.WhiteSmoke, flags);
            };

            // 4. Direction Wilaya (Pied du header - Sans ombre)
            lblWilaya = new Label
            {
                Text = " DTN - Relizane ",
                Font = new Font("Segoe UI", 9F, FontStyle.Italic),
                ForeColor = Color.Gray,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter,
                Left = 135,
                Top = 138,
                Width = 630,
                Height = 24
            };

            // Ajout des contrôles au panneau
            pnlHeader.Controls.Add(lblRepublique);
            pnlHeader.Controls.Add(picMinistere);
            pnlHeader.Controls.Add(picDirection);
            pnlHeader.Controls.Add(lblMinistere);
            pnlHeader.Controls.Add(lblDirection);
            pnlHeader.Controls.Add(lblWilaya);

            picDirection.BringToFront();
            picMinistere.BringToFront();

            // =========================================================
            // PANNEAU CENTRAL (FORMULAIRE DE CONNEXION)
            // =========================================================
            pnlContent = new Guna2Panel
            {
                Dock = DockStyle.Fill,
                FillColor = Color.White
            };

            lblTitreApp = new Label
            {
                Text = "CONNEXION AU SYSTÈME",
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = Color.Green,
                Left = 240,
                Top = 22,
                Width = 420,
                Height = 30,
                TextAlign = ContentAlignment.MiddleCenter
            };

            lblSousTitre = new Label
            {
                Text = "Gestion du Stock et des Équipements",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
                ForeColor = Color.FromArgb(100, 116, 139),
                Left = 240,
                Top = 56,
                Width = 420,
                TextAlign = ContentAlignment.MiddleCenter
            };

            int startX = 230;
            int inputWidth = 440;
            int y = 100;

            lblLogin = new Label
            {
                Text = "Identifiant",
                Left = startX,
                Top = y,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(51, 65, 85),
                AutoSize = true
            };
            y += 24;

            txtLogin = new Guna2TextBox
            {
                Left = startX,
                Top = y,
                Width = inputWidth,
                Height = 44,
                BorderRadius = 8,
                PlaceholderText = "Nom d'utilisateur ou matricule",
                Font = new Font("Segoe UI", 9.5F),
                BorderColor = Color.FromArgb(226, 232, 240),
                FocusedState = { BorderColor = Color.Green }
            };
            y += 58;

            lblMdp = new Label
            {
                Text = "Mot de passe",
                Left = startX,
                Top = y,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(51, 65, 85),
                AutoSize = true
            };
            y += 24;

            txtMdp = new Guna2TextBox
            {
                Left = startX,
                Top = y,
                Width = inputWidth,
                Height = 44,
                BorderRadius = 8,
                PlaceholderText = "••••••••",
                Font = new Font("Segoe UI", 9.5F),
                UseSystemPasswordChar = true,
                BorderColor = Color.FromArgb(226, 232, 240),
                FocusedState = { BorderColor = Color.Green }
            };
            txtMdp.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) BtnConnexion_Click(s, e); };
            y += 50;

            chkAfficherMdp = new Guna2CheckBox
            {
                Text = "Afficher le mot de passe",
                Left = startX,
                Top = y,
                Width = 250,
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.FromArgb(100, 116, 139)
            };
            chkAfficherMdp.CheckedChanged += (s, e) =>
                txtMdp.UseSystemPasswordChar = !chkAfficherMdp.Checked;
            y += 34;

            lblErreur = new Label
            {
                Left = startX,
                Top = y,
                Width = inputWidth,
                ForeColor = Color.FromArgb(220, 38, 38),
                Font = new Font("Segoe UI", 8.5F),
                TextAlign = ContentAlignment.MiddleCenter,
                Text = "",
                Visible = false
            };

            lblBlocage = new Label
            {
                Left = startX,
                Top = y,
                Width = inputWidth,
                ForeColor = Color.FromArgb(234, 88, 12),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Text = "",
                Visible = false
            };
            y += 26;

            btnConnexion = new Guna2Button
            {
                Left = startX,
                Top = y,
                Width = 290,
                Height = 46,
                Text = "SE CONNECTER",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.White,
                FillColor = Color.Green,
                BorderRadius = 8,
                Cursor = Cursors.Hand
            };
            btnConnexion.Click += BtnConnexion_Click;

            btnQuitter = new Guna2Button
            {
                Left = startX + 305,
                Top = y,
                Width = 135,
                Height = 46,
                Text = "Quitter",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(71, 85, 105),
                FillColor = Color.FromArgb(241, 245, 249),
                BorderRadius = 8,
                Cursor = Cursors.Hand
            };
            btnQuitter.Click += (s, e) =>
            {
                DialogResult = DialogResult.Cancel;
                Close();
            };

            pnlContent.Controls.Add(lblTitreApp);
            pnlContent.Controls.Add(lblSousTitre);
            pnlContent.Controls.Add(lblLogin);
            pnlContent.Controls.Add(txtLogin);
            pnlContent.Controls.Add(lblMdp);
            pnlContent.Controls.Add(txtMdp);
            pnlContent.Controls.Add(chkAfficherMdp);
            pnlContent.Controls.Add(lblErreur);
            pnlContent.Controls.Add(lblBlocage);
            pnlContent.Controls.Add(btnConnexion);
            pnlContent.Controls.Add(btnQuitter);

            Controls.Add(pnlContent);
            Controls.Add(pnlHeader);

            Load += (s, e) => txtLogin.Focus();
        }

        private static string HashSha256(string texte)
        {
            using var sha = SHA256.Create();
            byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(texte));
            return Convert.ToHexString(bytes).ToLower();
        }

        private void BtnConnexion_Click(object? sender, EventArgs e)
        {
            if (!btnConnexion.Enabled) return;

            string login = txtLogin.Text.Trim();
            string mdp = txtMdp.Text;

            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(mdp))
            {
                AfficherErreur("Veuillez remplir tous les champs.");
                return;
            }

            

            try
            {
                string hashSaisi = HashSha256(mdp);
                var t = DatabaseHelper.ExecuteQuery(
                    "SELECT id, nom_affichage FROM Utilisateur WHERE login=@login AND mot_de_passe_hash=@hash AND actif=1",
                    new SqliteParameter("@login", login),
                    new SqliteParameter("@hash", hashSaisi));

                if (t.Rows.Count > 0)
                {
                    SessionUtilisateur.Connecter(
                        Convert.ToInt32(t.Rows[0]["id"]),
                        login,
                        t.Rows[0]["nom_affichage"]?.ToString() ?? login);

                    DialogResult = DialogResult.OK;
                    Close();
                }
                else
                {
                    _tentatives++;
                    int restantes = MAX_TENTATIVES - _tentatives;

                    if (_tentatives >= MAX_TENTATIVES)
                    {
                        DemarrerBlocage();
                    }
                    else
                    {
                        AfficherErreur($"Identifiant ou mot de passe incorrect. ({restantes} tentative(s) restante(s))");
                        }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur de base de données : " + ex.Message, "Erreur SQL", MessageBoxButtons.OK, MessageBoxIcon.Error);
             
            }
        }

        private void AfficherErreur(string message)
        {
            lblErreur.Text = message;
            lblErreur.Visible = true;
            lblBlocage.Visible = false;

            txtMdp.BorderColor = Color.FromArgb(220, 38, 38);
            var timer = new System.Windows.Forms.Timer { Interval = 2000 };
            timer.Tick += (s, e) => { txtMdp.BorderColor = Color.FromArgb(226, 232, 240); timer.Stop(); };
            timer.Start();
        }

        private void DemarrerBlocage()
        {
            btnConnexion.Enabled = false;
            btnConnexion.FillColor = Color.FromArgb(156, 163, 175);
            lblErreur.Visible = false;
            lblBlocage.Visible = true;
            _secondesRestantes = BLOCAGE_SECONDES;

            _timerBlocage = new System.Windows.Forms.Timer { Interval = 1000 };
            _timerBlocage.Tick += (s, e) =>
            {
                _secondesRestantes--;
                lblBlocage.Text = $"Trop de tentatives. Réessayez dans {_secondesRestantes}s.";

                if (_secondesRestantes <= 0)
                {
                    _timerBlocage.Stop();
                    _tentatives = 0;
                    btnConnexion.Enabled = true;
                    btnConnexion.FillColor = Color.FromArgb(59, 130, 246);
                    lblBlocage.Visible = false;
                    txtMdp.Text = "";
                    txtMdp.Focus();
                }
            };

            lblBlocage.Text = $"Trop de tentatives. Réessayez dans {_secondesRestantes}s.";
            _timerBlocage.Start();
        }
    }
}