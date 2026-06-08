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
        /// Authenticates a user and enforces Single Active Session for mahasiswa.
        /// For mahasiswa: verifies password + device binding in one atomic DB operation.
        /// For dosen/panitia: falls back to standard authentication (no device binding).
        /// </summary>
        /// <returns>(Success, Message) — Message is shown directly in the UI on failure.</returns>
        public async Task<(bool Success, string Message)> LoginAsync(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                return (false, "Masukkan Username dan Password Anda");

            try
            {
                // --- Try mahasiswa path (includes device binding) ---
                var (success, message, user) = await _databaseService.LoginMahasiswaAsync(username, password);

                if (message != null) // user was found as mahasiswa (success OR explicit failure)
                {
                    if (!success)
                        return (false, message);

                    CurrentUser = user;
                    CurrentStudentId = username;

                    if (user!.MahasiswaId.HasValue)
                        CurrentMahasiswa = await _databaseService.GetMahasiswaByIdAsync(user.MahasiswaId.Value);

                    return (true, string.Empty);
                }

                // --- Fallback: dosen / panitia (no device binding) ---
                var regularUser = await _databaseService.AuthenticateUserAsync(username, password);
                if (regularUser != null)
                {
                    CurrentUser = regularUser;
                    CurrentStudentId = username;
                    return (true, string.Empty);
                }

                return (false, "Username atau Password salah");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoginAsync error: {ex.Message}");
                return (false, "Terjadi kesalahan saat login. Silakan coba lagi.");
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
        /// Logs out the current user (synchronous, in-memory only).
        /// Prefer LogoutAsync() when called from async contexts.
        /// </summary>
        public void Logout()
        {
            CurrentUser = null;
            CurrentMahasiswa = null;
            CurrentStudentId = null;
        }

        /// <summary>
        /// Releases the device binding in the database (mahasiswa only), then clears in-memory state.
        /// Always call this instead of Logout() when an await is available.
        /// </summary>
        public async Task LogoutAsync()
        {
            if (CurrentUser?.Level == "mahasiswa" && !string.IsNullOrEmpty(CurrentUser.Username))
            {
                await _databaseService.LogoutMahasiswaAsync(CurrentUser.Username);
            }
            Logout();
        }
    }
}
