namespace InventoryApp
{
    /// <summary>
    /// Singleton statique qui garde en mémoire l'utilisateur connecté
    /// pendant toute la durée de la session (de la connexion jusqu'à
    /// la fermeture du programme). Accessible depuis n'importe quelle
    /// classe sans passer de paramètre.
    /// </summary>
    public static class SessionUtilisateur
    {
        public static int Id { get; private set; }
        public static string Login { get; private set; } = "";
        public static string NomAffichage { get; private set; } = "";
        public static bool EstConnecte { get; private set; } = false;

        public static void Connecter(int id, string login, string nomAffichage)
        {
            Id = id;
            Login = login;
            NomAffichage = nomAffichage;
            EstConnecte = true;
        }

        public static void Deconnecter()
        {
            Id = 0;
            Login = "";
            NomAffichage = "";
            EstConnecte = false;
        }
    }
}