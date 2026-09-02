using System;
using System.Data;
using Microsoft.Data.Sqlite;

namespace InventoryApp.Data
{
    public static class DatabaseHelper
    {
        // Le chemin est relatif au dossier ou tourne l'application (bin/Debug/...),
        // c'est pour ca que le fichier .db doit avoir "Copy if newer" active.
        private static readonly string ConnectionString = "Data Source=Data/gestion_equipement.db";

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
    }
}
