using System.Windows;
using System.Windows.Input;
using CBTSecureDesktop.Services;

namespace CBTSecureDesktop.UI
{
    public partial class LoginWindow : Window
    {
        private readonly AuthService _authService;

        public LoginWindow()
        {
            InitializeComponent();
            _authService = new AuthService();
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            await PerformLogin();
        }

        private void PasswordBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                _ = PerformLogin();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private async Task PerformLogin()
        {
            string studentId = StudentIdTextBox.Text.Trim();
            string password = PasswordBox.Password;

            // Clear previous error
            ErrorMessage.Visibility = Visibility.Collapsed;

            // Validate input
            if (string.IsNullOrWhiteSpace(studentId))
            {
                ShowError("Masukkan Username Anda");
                return;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                ShowError("Masukkan Password Anda");
                return;
            }

            // Show loading state
            LoginButton.IsEnabled = false;
            LoadingPanel.Visibility = Visibility.Visible;

            try
            {
                // Authenticate (with device binding for mahasiswa)
                var (isAuthenticated, message) = await _authService.LoginAsync(studentId, password);

                if (isAuthenticated)
                {
                    _authService.SetCurrentStudent(studentId);

                    // Open Dashboard
                    var dashboard = new DashboardWindow(_authService);
                    dashboard.Show();
                    this.Close();
                }
                else
                {
                    ShowError(string.IsNullOrEmpty(message) ? "Username atau Password salah" : message);
                }
            }
            catch (Exception ex)
            {
                ShowError($"Login gagal: {ex.Message}");
            }
            finally
            {
                LoginButton.IsEnabled = true;
                LoadingPanel.Visibility = Visibility.Collapsed;
            }
        }

        private void ShowError(string message)
        {
            ErrorMessage.Text = message;
            ErrorMessage.Visibility = Visibility.Visible;
        }
    }
}
