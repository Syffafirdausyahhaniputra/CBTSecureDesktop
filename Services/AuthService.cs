using CBTSecureDesktop.Data;
using CBTSecureDesktop.Models;

namespace CBTSecureDesktop.Services
{
    /// <summary>
    /// Handles user authentication for the CBT system using MySQL database.
    /// </summary>
    public class AuthService
    {
        private readonly DatabaseService _databaseService;

        public AuthService()
        {
            _databaseService = new DatabaseService();
        }

        /// <summary>
        /// Authenticates a user using their credentials from database.
        /// </summary>
        /// <param name="username">Username (NIM or NIP)</param>
        /// <param name="password">User password</param>
        /// <returns>True if authentication successful, false otherwise</returns>
        public async Task<bool> AuthenticateAsync(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                return false;
            }

            try
            {
                // Authenticate against database
                var user = await _databaseService.AuthenticateUserAsync(username, password);

                if (user != null)
                {
                    CurrentUser = user;
                    CurrentStudentId = username;

                    // If user is a student, get student details
                    if (user.Level == "mahasiswa" && user.MahasiswaId.HasValue)
                    {
                        CurrentMahasiswa = await _databaseService.GetMahasiswaByIdAsync(user.MahasiswaId.Value);
                    }

                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Authentication error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gets the currently authenticated user.
        /// </summary>
        public User? CurrentUser { get; private set; }

        /// <summary>
        /// Gets the currently authenticated student details.
        /// </summary>
        public Mahasiswa? CurrentMahasiswa { get; private set; }

        /// <summary>
        /// Gets the currently authenticated student ID (for backward compatibility).
        /// </summary>
        public string? CurrentStudentId { get; private set; }

        /// <summary>
        /// Sets the current authenticated student.
        /// </summary>
        public void SetCurrentStudent(string studentId)
        {
            CurrentStudentId = studentId;
        }

        /// <summary>
        /// Changes the password for the currently authenticated user.
        /// </summary>
        public async Task<(bool Success, string Message)> ChangePasswordAsync(string currentPassword, string newPassword)
        {
            if (CurrentUser == null)
                return (false, "User belum login.");

            var result = await _databaseService.ChangeUserPasswordAsync(CurrentUser.UserId, currentPassword, newPassword);
            if (result.Success)
            {
                CurrentUser.Password = newPassword;
            }

            return result;
        }

        /// <summary>
        /// Logs out the current user.
        /// </summary>
        public void Logout()
        {
            CurrentUser = null;
            CurrentMahasiswa = null;
            CurrentStudentId = null;
        }
    }
}
