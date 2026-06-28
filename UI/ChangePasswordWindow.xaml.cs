using System.Windows;
using System.Windows.Controls;
using CBTSecureDesktop.Services;

namespace CBTSecureDesktop.UI
{
    public partial class ChangePasswordWindow : Window
    {
        private readonly AuthService _authService;

        // Flag pengaman agar sinkronisasi teks tidak mengalami infinite loop
        private bool _isSyncing = false;

        public ChangePasswordWindow(AuthService authService)
        {
            InitializeComponent();
            _authService = authService;
        }

        // ==========================================
        // Logika Toggle Mata (Show/Hide Password)
        // ==========================================
        private void ToggleCurrentBtn_Click(object sender, RoutedEventArgs e)
        {
            TogglePasswordVisibility(ToggleCurrentBtn.IsChecked == true, CurrentPasswordBox, CurrentTextBox, IconCurrent);
        }

        private void ToggleNewBtn_Click(object sender, RoutedEventArgs e)
        {
            TogglePasswordVisibility(ToggleNewBtn.IsChecked == true, NewPasswordBox, NewTextBox, IconNew);
        }

        private void ToggleConfirmBtn_Click(object sender, RoutedEventArgs e)
        {
            TogglePasswordVisibility(ToggleConfirmBtn.IsChecked == true, ConfirmPasswordBox, ConfirmTextBox, IconConfirm);
        }

        private void TogglePasswordVisibility(bool isShow, PasswordBox pBox, System.Windows.Controls.TextBox tBox, TextBlock icon)
        {
            if (isShow)
            {
                tBox.Visibility = Visibility.Visible;
                pBox.Visibility = Visibility.Collapsed;
                icon.Text = "\uE8D4"; // Mata dicoret
            }
            else
            {
                pBox.Visibility = Visibility.Visible;
                tBox.Visibility = Visibility.Collapsed;
                icon.Text = "\uE890"; // Mata terbuka
            }
        }

        // ==========================================
        // Logika Sinkronisasi Ketikan (Real-time)
        // ==========================================
        private void CurrentPasswordBox_PasswordChanged(object sender, RoutedEventArgs e) => SyncPasswords(CurrentPasswordBox, CurrentTextBox, true);
        private void CurrentTextBox_TextChanged(object sender, TextChangedEventArgs e) => SyncPasswords(CurrentPasswordBox, CurrentTextBox, false);

        private void NewPasswordBox_PasswordChanged(object sender, RoutedEventArgs e) => SyncPasswords(NewPasswordBox, NewTextBox, true);
        private void NewTextBox_TextChanged(object sender, TextChangedEventArgs e) => SyncPasswords(NewPasswordBox, NewTextBox, false);

        private void ConfirmPasswordBox_PasswordChanged(object sender, RoutedEventArgs e) => SyncPasswords(ConfirmPasswordBox, ConfirmTextBox, true);
        private void ConfirmTextBox_TextChanged(object sender, TextChangedEventArgs e) => SyncPasswords(ConfirmPasswordBox, ConfirmTextBox, false);

        private void SyncPasswords(PasswordBox pBox, System.Windows.Controls.TextBox tBox, bool isFromPasswordBox)
        {
            if (_isSyncing) return;
            _isSyncing = true;

            if (isFromPasswordBox)
            {
                tBox.Text = pBox.Password;
            }
            else
            {
                pBox.Password = tBox.Text;
            }

            _isSyncing = false;
        }

        // ==========================================
        // Logika Tombol Aksi (Simpan & Batal)
        // ==========================================
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Karena tersinkron, kita cukup mengambil nilainya dari PasswordBox saja
            var currentPassword = CurrentPasswordBox.Password;
            var newPassword = NewPasswordBox.Password;
            var confirmPassword = ConfirmPasswordBox.Password;

            ErrorText.Visibility = Visibility.Collapsed;
            ErrorText.Text = string.Empty;

            if (string.IsNullOrWhiteSpace(currentPassword))
            {
                ShowError("Password saat ini wajib diisi.");
                return;
            }

            if (string.IsNullOrWhiteSpace(newPassword))
            {
                ShowError("Password baru wajib diisi.");
                return;
            }

            if (newPassword.Length < 6)
            {
                ShowError("Password baru minimal 6 karakter.");
                return;
            }

            if (!string.Equals(newPassword, confirmPassword))
            {
                ShowError("Konfirmasi password baru tidak sama.");
                return;
            }

            SaveButton.IsEnabled = false;
            try
            {
                var result = await _authService.ChangePasswordAsync(currentPassword, newPassword);
                if (result.Success)
                {
                    MessageBox.Show(result.Message, "Berhasil", MessageBoxButton.OK, MessageBoxImage.Information);
                    Close();
                    return;
                }

                ShowError(result.Message);
            }
            catch (Exception ex)
            {
                ShowError($"Gagal mengubah password: {ex.Message}");
            }
            finally
            {
                SaveButton.IsEnabled = true;
            }
        }

        private void ShowError(string message)
        {
            ErrorText.Text = message;
            ErrorText.Visibility = Visibility.Visible;
        }
    }
}