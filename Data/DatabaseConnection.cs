using MySqlConnector;
using System;
using System.Threading.Tasks;
using CBTSecureDesktop.Configuration;

namespace CBTSecureDesktop.Data
{
    /// <summary>
    /// Manages database connection to MySQL/MariaDB (Laragon)
    /// </summary>
    public class DatabaseConnection
    {
        private static DatabaseConnection? _instance;

        private DatabaseConnection()
        {
            // Konstruktor sekarang dikosongkan. 
            // Kita tidak lagi menyimpan connection string secara telanjang di sini.
        }

        /// <summary>
        /// Gets the singleton instance of DatabaseConnection
        /// </summary>
        public static DatabaseConnection Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new DatabaseConnection();
                }
                return _instance;
            }
        }

        /// <summary>
        /// Creates and returns a new MySQL connection
        /// </summary>
        public MySqlConnection GetConnection()
        {
            string secureConnString = AppConfig.GetDatabaseConnectionString();
            return new MySqlConnection(secureConnString);
        }

        /// <summary>
        /// Tests the database connection
        /// </summary>
        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                using var connection = GetConnection();
                await connection.OpenAsync();
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Database connection failed: {ex.Message}");
                return false;
            }
        }
    }
}