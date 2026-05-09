using System.Windows;
using CBTSecureDesktop.Services;
using CBTSecureDesktop.Models;

namespace CBTSecureDesktop.UI
{
    public partial class DashboardWindow : Window
    {
        private readonly AuthService _authService;
        private readonly ExamService _examService;

        public DashboardWindow(AuthService authService)
        {
            InitializeComponent();
            _authService = authService;
            _examService = new ExamService();

            Loaded += DashboardWindow_Loaded;
        }

        private async void DashboardWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Set student name
            if (_authService.CurrentMahasiswa != null)
            {
                StudentNameText.Text = _authService.CurrentMahasiswa.Nama;
            }
            else if (!string.IsNullOrEmpty(_authService.CurrentStudentId))
            {
                StudentNameText.Text = _authService.CurrentStudentId;
            }

            await LoadExams();
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Apakah Anda yakin ingin keluar?",
                "Konfirmasi Logout",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _authService.Logout();
                var loginWindow = new LoginWindow();
                loginWindow.Show();
                this.Close();
            }
        }

        private async Task LoadExams()
        {
            try
            {
                LoadingPanel.Visibility = Visibility.Visible;
                ExamsListView.Visibility = Visibility.Collapsed;

                // Get mahasiswa ID from authenticated user
                long mahasiswaId = _authService.CurrentUser?.MahasiswaId ?? 0;

                if (mahasiswaId > 0)
                {
                    var exams = await _examService.GetAvailableExamsAsync(mahasiswaId);
                    ExamsListView.ItemsSource = exams;

                    LoadingPanel.Visibility = Visibility.Collapsed;
                    ExamsListView.Visibility = Visibility.Visible;
                }
                else
                {
                    MessageBox.Show("Student information not found. Please login again.", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load exams: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void StartExamButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not System.Windows.Controls.Button button) return;

            // Check if it's a start or review action based on the datacontext 
            var ujian = button.DataContext as Ujian;
            if (ujian == null) return;

            long ujianId = ujian.UjianId;

            if (ujian.StatusMahasiswa == "selesai")
            {
                // Here we would implement the review exam window
                // For now, let's just show a temporary message
                MessageBox.Show(
                    $"Review mode untuk ujian '{ujian.NamaUjian}' akan segera tersedia.\n" +
                    $"Nilai Anda: {ujian.Nilai ?? 0}",
                    "Review Ujian",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            // Confirm before starting exam
            var result = MessageBox.Show(
                "Once the exam starts, you will enter secure mode.\n\n" +
                "• Task switching will be disabled\n" +
                "• System shortcuts will be blocked\n" +
                "• You cannot exit until the exam is submitted\n\n" +
                "Do you want to start the exam?",
                "Start Exam - Security Warning",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                // Get mahasiswa ID
                long mahasiswaId = _authService.CurrentUser?.MahasiswaId ?? 0;

                if (mahasiswaId > 0)
                {
                    // Open Exam Window with database IDs
                    var examWindow = new ExamWindow(_authService, _examService, ujianId, mahasiswaId);
                    examWindow.Show();
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Informasi mahasiswa tidak ditemukan. Silakan login kembali.", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            if (RefreshButton.IsEnabled)
            {
                RefreshButton.IsEnabled = false;
                RefreshButton.Content = "Memuat...";

                await LoadExams();

                // Simple cooldown to prevent spamming
                await Task.Delay(2000); 
                RefreshButton.IsEnabled = true;
                RefreshButton.Content = "🔄 Refresh";
            }
        }
    }
}
