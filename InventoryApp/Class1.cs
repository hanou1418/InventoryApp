using System.Data;
using Microsoft.Data.Sqlite;

namespace InventoryApp.Data
{
    public static class DatabaseHelper
    {
        // ---------------------------------------------------------------
        // SÉCURISATION : la base ne vit plus dans le dossier d'installation
        // (bin/Debug/Data/...) mais dans le dossier utilisateur AppData,
        // qu'AUCUN désinstalleur ne supprime jamais automatiquement.
        //
        //   Windows : C:\Users\<utilisateur>\AppData\Local\InventoryApp\gestion_equipement.db
        // ---------------------------------------------------------------
        private static readonly string DossierDonnees = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "InventoryApp");

        private static readonly string DossierSauvegardes = Path.Combine(DossierDonnees, "Sauvegardes");

        private static readonly string CheminBaseDonnees = Path.Combine(DossierDonnees, "gestion_equipement_migree.db");

        private static readonly string ConnectionString = $"Data Source={CheminBaseDonnees}";

        // Le constructeur statique s'exécute une seule fois, avant le tout premier
        // appel à DatabaseHelper (GetConnection, ExecuteQuery, etc.) — donc avant
        // même l'ouverture de Form1.
        static DatabaseHelper()
        {
            InitialiserEmplacementBaseDonnees();

            // Sauvegarde automatique au démarrage — au maximum une fois par 24h,
            // pour ne pas créer une sauvegarde à chaque ouverture de l'app dans la journée.
            SauvegarderAutomatiquementSiNecessaire(TimeSpan.FromHours(24));
        }

        // Crée le dossier AppData et, si c'est le tout premier lancement,
        // copie la base "modèle" livrée avec l'application (Data/gestion_equipement.db,
        // celle qui a "Copy if newer" dans les propriétés du fichier) vers l'emplacement sécurisé.
        // Les lancements suivants réutilisent la base déjà présente dans AppData
        // et ne touchent plus jamais au fichier livré avec l'application.
        private static void InitialiserEmplacementBaseDonnees()
        {
            Directory.CreateDirectory(DossierDonnees);
            Directory.CreateDirectory(DossierSauvegardes);

            if (!File.Exists(CheminBaseDonnees))
            {
                string cheminModele = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "gestion_equipement_migree.db");

                if (File.Exists(cheminModele))
                {
                    File.Copy(cheminModele, CheminBaseDonnees);
                    System.Diagnostics.Debug.WriteLine("Base de données initialisée dans : " + CheminBaseDonnees);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("ATTENTION : aucun fichier modèle trouvé à " + cheminModele);
                }
            }
        }

        // Permet d'afficher/loguer où se trouve réellement la base (utile pour un écran "À propos"
        // ou un bouton "Ouvrir le dossier des données").
        public static string ObtenirCheminBaseDonnees() => CheminBaseDonnees;
        public static string ObtenirDossierSauvegardes() => DossierSauvegardes;

        public static SqliteConnection GetConnection()
        {
            var conn = new SqliteConnection(ConnectionString);
            conn.Open();

            // IMPORTANT : sans cette ligne, les FOREIGN KEY et donc certains
            // comportements du schema ne sont pas actives par defaut sur
            // chaque nouvelle connexion (contrairement au script .sql original
            // qui l'active une seule fois a la creation).
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "PRAGMA foreign_keys = ON;";
                cmd.ExecuteNonQuery();
            }

            return conn;
        }

        // Utilitaire generique : execute un SELECT et retourne un DataTable,
        // pratique pour alimenter directement un DataGridView (guna2DataGridView).
        public static DataTable ExecuteQuery(string sql, params SqliteParameter[] parameters)
        {
            var table = new DataTable();
            try
            {
                using (var conn = GetConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = sql;
                    if (parameters != null)
                        cmd.Parameters.AddRange(parameters);

                    using (var reader = cmd.ExecuteReader())
                    {
                        table.Load(reader);
                    }
                }
            }
            catch (SqliteException ex)
            {
                // Affiche la requête exacte qui a échoué dans la fenêtre 'Sortie' (Output) de Visual Studio
                System.Diagnostics.Debug.WriteLine("=== ERREUR SQLITE ===");
                System.Diagnostics.Debug.WriteLine("Requête : " + sql);
                System.Diagnostics.Debug.WriteLine("Message : " + ex.Message);
                throw;
            }
            return table;
        }

        // Utilitaire generique : execute un INSERT/UPDATE/DELETE.
        // Retourne le nombre de lignes affectees.
        public static int ExecuteNonQuery(string sql, params SqliteParameter[] parameters)
        {
            using (var conn = GetConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = sql;
                if (parameters != null)
                    cmd.Parameters.AddRange(parameters);

                return cmd.ExecuteNonQuery();
            }
        }

        public static DataTable ExecuteQueryWithParams(string sql, Dictionary<string, object> parameters)
        {
            DataTable dt = new DataTable();
            using (var conn = new SqliteConnection(ConnectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = sql;
                    if (parameters != null)
                    {
                        foreach (var p in parameters)
                        {
                            cmd.Parameters.AddWithValue(p.Key, p.Value ?? DBNull.Value);
                        }
                    }
                    using (var reader = cmd.ExecuteReader())
                    {
                        dt.Load(reader);
                    }
                }
            }
            return dt;
        }

        // ---------------------------------------------------------------
        // SAUVEGARDE / RESTAURATION
        // ---------------------------------------------------------------

        // Copie le fichier .db actuel vers le dossier de sauvegardes, horodaté.
        // À appeler par exemple : à la fermeture de Form1, sur un Timer quotidien,
        // ou via un bouton "Sauvegarder maintenant" dans l'interface.
        public static string SauvegarderBaseDonnees(int nombreMaxSauvegardes = 15)
        {
            Directory.CreateDirectory(DossierSauvegardes);

            string nomFichier = $"gestion_equipement_migree_{DateTime.Now:yyyyMMdd_HHmmss}.db";
            string cheminBackup = Path.Combine(DossierSauvegardes, nomFichier);

            // SQLite déconseille de copier le fichier pendant une écriture en cours ;
            // VACUUM INTO garantit une copie propre et cohérente, même si l'appli tourne.
            using (var conn = GetConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "VACUUM INTO $chemin;";
                cmd.Parameters.AddWithValue("$chemin", cheminBackup);
                cmd.ExecuteNonQuery();
            }

            NettoyerAnciennesSauvegardes(nombreMaxSauvegardes);

            return cheminBackup;
        }

        // Sauvegarde automatique et SILENCIEUSE : ne se déclenche que si la dernière
        // sauvegarde date de plus de "intervalleMinimum". Aucune action de l'utilisateur requise.
        public static void SauvegarderAutomatiquementSiNecessaire(TimeSpan intervalleMinimum)
        {
            try
            {
                Directory.CreateDirectory(DossierSauvegardes);

                var derniereSauvegarde = new DirectoryInfo(DossierSauvegardes)
                    .GetFiles("gestion_equipement_migree_*.db")
                    .OrderByDescending(f => f.CreationTimeUtc)
                    .FirstOrDefault();

                bool doitSauvegarder = derniereSauvegarde == null
                    || (DateTime.UtcNow - derniereSauvegarde.CreationTimeUtc) >= intervalleMinimum;

                if (doitSauvegarder)
                    SauvegarderBaseDonnees();
            }
            catch (Exception ex)
            {
                // Une sauvegarde ratée ne doit jamais empêcher l'application de démarrer.
                System.Diagnostics.Debug.WriteLine("Sauvegarde automatique échouée : " + ex.Message);
            }
        }

        // Ne garde que les N sauvegardes les plus récentes, pour ne pas remplir le disque indéfiniment.
        private static void NettoyerAnciennesSauvegardes(int nombreMaxSauvegardes)
        {
            var fichiers = new DirectoryInfo(DossierSauvegardes)
                .GetFiles("gestion_equipement_migree_*.db")
                .OrderByDescending(f => f.CreationTimeUtc)
                .ToList();

            for (int i = nombreMaxSauvegardes; i < fichiers.Count; i++)
            {
                try { fichiers[i].Delete(); } catch { /* fichier verrouillé, on réessaiera plus tard */ }
            }
        }

        // Restaure une sauvegarde choisie (écrase la base active).
        // À utiliser avec précaution : fermer toutes les connexions ouvertes avant d'appeler ceci.
        public static void RestaurerSauvegarde(string cheminFichierSauvegarde)
        {
            if (!File.Exists(cheminFichierSauvegarde))
                throw new FileNotFoundException("Fichier de sauvegarde introuvable.", cheminFichierSauvegarde);

            SqliteConnection.ClearAllPools(); // libère les verrous SQLite avant d'écraser le fichier
            File.Copy(cheminFichierSauvegarde, CheminBaseDonnees, overwrite: true);
        }

        public static List<string> ListerSauvegardes()
        {
            Directory.CreateDirectory(DossierSauvegardes);
            return new DirectoryInfo(DossierSauvegardes)
                .GetFiles("gestion_equipement_migree_*.db")
                .OrderByDescending(f => f.CreationTimeUtc)
                .Select(f => f.FullName)
                .ToList();
        }
    }
}