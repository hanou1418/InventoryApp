using System;
using System.Windows.Forms;

namespace InventoryApp
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            //Application.Run(new Form1());
            // 1. Afficher l'écran de connexion
            using (var login = new FrmLogin())
            {
                // Si l'utilisateur ferme sans se connecter -> on quitte
                if (login.ShowDialog() != DialogResult.OK || !SessionUtilisateur.EstConnecte)
                    return;
            }

            // 2. Connexion réussie -> ouvrir l'application principale
            Application.Run(new Form1());
        }
    }
}