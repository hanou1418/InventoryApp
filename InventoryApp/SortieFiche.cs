using System.Diagnostics;

namespace InventoryApp
{
    /// <summary>
    /// Gère la sortie d'une fiche HTML : impression (navigateur) OU enregistrement en PDF.
    /// Le PDF est généré avec Microsoft Edge en mode "headless" (présent sur Windows 10/11),
    /// donc aucun package NuGet à installer. L'arabe (RTL) et le CSS sont conservés.
    /// </summary>
    internal static class SortieFiche
    {
        public static void Proposer(string fichierHtml, string nomPdfSuggere)
        {
            var choix = MessageBox.Show(
                "Que voulez-vous faire avec cette fiche ?\n\n" +
                "Oui  = Imprimer\n" +
                "Non  = Enregistrer en PDF",
                "Imprimer / PDF",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question);

            if (choix == DialogResult.Yes)
            {
                Process.Start(new ProcessStartInfo(fichierHtml) { UseShellExecute = true });
            }
            else if (choix == DialogResult.No)
            {
                EnregistrerPdf(fichierHtml, nomPdfSuggere);
            }
        }

        private static void EnregistrerPdf(string fichierHtml, string nomPdfSuggere)
        {
            using var dlg = new SaveFileDialog
            {
                Filter = "Fichier PDF (*.pdf)|*.pdf",
                FileName = nomPdfSuggere,
                DefaultExt = "pdf",
                AddExtension = true
            };

            if (dlg.ShowDialog() != DialogResult.OK) return;

            string? edge = TrouverEdge();
            if (edge == null)
            {
                MessageBox.Show("Microsoft Edge est introuvable sur ce poste.\n" +
                                "La fiche va s'ouvrir dans le navigateur : utilisez Ctrl+P puis « Enregistrer au format PDF ».",
                                "PDF", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                Process.Start(new ProcessStartInfo(fichierHtml) { UseShellExecute = true });
                return;
            }

            // Profil temporaire pour ne pas gêner un Edge déjà ouvert
            string profil = Path.Combine(Path.GetTempPath(), "edge_pdf_" + Guid.NewGuid().ToString("N"));
            string url = new Uri(fichierHtml).AbsoluteUri;

            var psi = new ProcessStartInfo(edge)
            {
                UseShellExecute = false,
                CreateNoWindow = true
            };
            psi.ArgumentList.Add("--headless");
            psi.ArgumentList.Add("--disable-gpu");
            psi.ArgumentList.Add("--no-pdf-header-footer");
            psi.ArgumentList.Add("--user-data-dir=" + profil);
            psi.ArgumentList.Add("--print-to-pdf=" + dlg.FileName);
            psi.ArgumentList.Add(url);

            try
            {
                if (File.Exists(dlg.FileName)) File.Delete(dlg.FileName);

                using var p = Process.Start(psi)!;
                if (!p.WaitForExit(30000))
                {
                    try { p.Kill(true); } catch { }
                }

                if (File.Exists(dlg.FileName))
                {
                    Process.Start(new ProcessStartInfo(dlg.FileName) { UseShellExecute = true });
                }
                else
                {
                    MessageBox.Show("Le PDF n'a pas pu être créé.", "PDF",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur lors de la création du PDF :\n" + ex.Message, "PDF",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                try { if (Directory.Exists(profil)) Directory.Delete(profil, true); } catch { }
            }
        }

        private static string? TrouverEdge()
        {
            string[] chemins =
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), @"Microsoft\Edge\Application\msedge.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),    @"Microsoft\Edge\Application\msedge.exe")
            };
            return chemins.FirstOrDefault(File.Exists);
        }
    }
}