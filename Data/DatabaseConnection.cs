using MySqlConnector;
using System.Configuration;

namespace CBTSecureDesktop.Data
{
    /// <summary>
    /// Manages database connection to MySQL/MariaDB (Laragon)
    /// </summary>
    public class DatabaseConnection
    {
        private static DatabaseConnection? _instance;
        private readonly string _connectionString;

        private DatabaseConnection()
        {
            // Connection string for Laragon local database
            _connectionString = "Server=localhost;Port=3306;Database=cbt_database;User Id=root;Password=;";
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
            return new MySqlConnection(_connectionString);
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
