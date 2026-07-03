using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using CBTSecureDesktop.Services;
using CBTSecureDesktop.Models;

namespace CBTSecureDesktop.UI
{
    public partial class DashboardWindow : Window
    {
        private readonly AuthService _authService;
        private readonly ExamService _examService;
        private readonly ImageService _imageService;
        private readonly HashSet<long> _preparedExamIds = new();
        private int _preparationStatusToken = 0;

        public DashboardWindow(AuthService authService)
        {
            InitializeComponent();
            _authService = authService;
            _examService = new ExamService();
            _imageService = new ImageService();

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

        private void ChangePasswordButton_Click(object sender, RoutedEventArgs e)
        {
            var window = new ChangePasswordWindow(_authService)
            {
                Owner = this
            };

            window.ShowDialog();
        }

        private async void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Apakah Anda yakin ingin keluar?",
                "Konfirmasi Logout",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                await _authService.LogoutAsync();
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
                ExamsListContainer.Visibility = Visibility.Collapsed;

                // Get mahasiswa ID from authenticated user
                long mahasiswaId = _authService.CurrentUser?.MahasiswaId ?? 0;

                if (mahasiswaId > 0)
                {
                    var exams = await _examService.GetAvailableExamsAsync(mahasiswaId);
                    var orderedExams = exams
                        .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt ?? DateTime.MinValue)
                        .ThenByDescending(x => x.StartTime)
                        .ToList();

                    ExamsListView.ItemsSource = orderedExams;

                    LoadingPanel.Visibility = Visibility.Collapsed;
                    ExamsListContainer.Visibility = Visibility.Visible;
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

        private async void PersiapanButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not System.Windows.Controls.Button button) return;

            var ujian = button.DataContext as Ujian;
            if (ujian == null) return;

            long ujianId = ujian.UjianId;

            button.IsEnabled = false;
            LoadingPanel.Visibility = Visibility.Collapsed;
            DownloadPanel.Visibility = Visibility.Visible;

            try
            {
                Random random = new Random();
                int delayMs = random.Next(5000, 30001);
                int delaySeconds = delayMs / 1000;

                DownloadProgressBar.IsIndeterminate = true;
                DownloadProgressText.Text = $"Menunggu antrean server (estimasi {delaySeconds} detik)...";

                await Task.Delay(delayMs);

                DownloadProgressBar.IsIndeterminate = false;
                DownloadProgressText.Text = "Menghubungkan ke server untuk mengambil daftar gambar soal dan opsi...";

                var questionImageIds = await _examService.GetAllImageIdsForExamAsync(ujianId);
                var optionImageIds = await _examService.GetAllOptionImageIdsForExamAsync(ujianId);
                int totalImages = questionImageIds.Count + optionImageIds.Count;

                if (totalImages == 0)
                {
                    await ShowPreparationStatusAsync(
                        "Persiapan ujian selesai. Tidak ada gambar soal maupun opsi yang perlu diunduh.",
                        new SolidColorBrush(Color.FromRgb(30, 58, 138)));
                    _preparedExamIds.Add(ujianId);
                    return;
                }

                DownloadProgressBar.Maximum = totalImages;
                DownloadProgressBar.Value = 0;
                int downloaded = 0;

                foreach (var imageId in questionImageIds)
                {
                    downloaded++;
                    Dispatcher.Invoke(() =>
                    {
                        DownloadProgressBar.Value = downloaded;
                        DownloadProgressText.Text = $"Mengunduh gambar soal: {downloaded} dari {totalImages}...";
                    });

                    await _imageService.GetImageAsync(imageId, "abc123");
                }

                foreach (var optionId in optionImageIds)
                {
                    downloaded++;
                    Dispatcher.Invoke(() =>
                    {
                        DownloadProgressBar.Value = downloaded;
                        DownloadProgressText.Text = $"Mengunduh gambar opsi: {downloaded} dari {totalImages}...";
                    });

                    await _imageService.GetOptionImageAsync(optionId, "abc123");
                }

                var failures = ImageService.ConsumeDownloadFailures();
                if (failures.Length > 0)
                {
                    ShowImageDownloadFailures(failures, "selama persiapan");
                }
                else
                {
                    await ShowPreparationStatusAsync(
                        "Persiapan ujian selesai. Semua gambar soal dan opsi berhasil diunduh.",
                        new SolidColorBrush(Color.FromRgb(34, 197, 94)));
                }

                _preparedExamIds.Add(ujianId);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Terjadi kesalahan saat mengunduh gambar: {ex.Message}", "Kesalahan Persiapan", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                button.IsEnabled = true;
                DownloadPanel.Visibility = Visibility.Collapsed;
                ExamsListContainer.Visibility = Visibility.Visible;
            }
        }

        private async void StartExamButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not System.Windows.Controls.Button button) return;

            // Check if it's a start or review action based on the datacontext 
            var ujian = button.DataContext as Ujian;
            if (ujian == null) return;

            long ujianId = ujian.UjianId;

            if (ujian.ActionText == "Menunggu Ujian" || ujian.ActionText == "Persiapan Ujian")
            {
                MessageBox.Show(
                    $"Ujian belum dimulai.",
                    "Informasi",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            if (ujian.ActionText == "Review Ujian")
            {
                // Here we would implement the review exam window
                // For now, let's just show a temporary message
                string stopMsg = ujian.StatusMahasiswa == "dihentikan" ? "\n\n(Ujian ini telah dihentikan oleh Admin/Proctor)" : "";

                MessageBox.Show(
                    $"Review mode untuk ujian '{ujian.NamaUjian}' akan segera tersedia.\n" +
                    $"Nilai Anda: {ujian.Nilai ?? 0}{stopMsg}",
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
                if (!_preparedExamIds.Contains(ujianId))
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            var imageIds = await _examService.GetAllImageIdsForExamAsync(ujianId);
                            Random random = new Random();
                            foreach (var imageId in imageIds)
                            {
                                await _imageService.GetImageAsync(imageId, "abc123");
                                int delayMs = random.Next(10000, 30001);
                                await Task.Delay(delayMs);
                            }

                            var failures = ImageService.ConsumeDownloadFailures();
                            if (failures.Length > 0)
                            {
                                Dispatcher.Invoke(() => ShowImageDownloadFailures(failures, "selama background download ujian"));
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Background download error: {ex.Message}");
                        }
                    });
                }

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

        private async Task ShowPreparationStatusAsync(string message, System.Windows.Media.Brush brush)
        {
            var token = ++_preparationStatusToken;
            PreparationStatusText.Text = message;
            PreparationStatusText.Foreground = brush;
            PreparationStatusText.Visibility = Visibility.Visible;

            await Task.Delay(4500);

            if (token == _preparationStatusToken)
            {
                PreparationStatusText.Text = string.Empty;
                PreparationStatusText.Visibility = Visibility.Collapsed;
            }
        }

        private void ShowImageDownloadFailures(IReadOnlyCollection<string> failures, string context)
        {
            if (failures.Count == 0)
            {
                return;
            }

            var message = "Beberapa gambar terkendala " + context + ":\n\n" +
                          string.Join("\n", failures.Select(x => "• " + x)) +
                          "\n\nGambar yang gagal akan tetap ditampilkan memakai placeholder.";

            MessageBox.Show(message, "Peringatan Unduhan Gambar", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private static int GetExamDisplayPriority(Ujian ujian)
        {
            var now = DateTime.Now;
            var statusGlobal = (ujian.Status ?? string.Empty).Trim().ToLowerInvariant();
            var statusMahasiswa = (ujian.StatusMahasiswa ?? string.Empty).Trim().ToLowerInvariant();

            bool isFinishedByStudent = statusMahasiswa == "selesai" || statusMahasiswa == "dihentikan";
            bool isExpired = statusGlobal == "selesai" || now > ujian.ActualEndTime;
            bool isWaitingStart = now < ujian.StartTime;
            bool isInProgress = statusMahasiswa == "dimulai";
            bool isNotStartedByStudent = string.IsNullOrEmpty(statusMahasiswa) || statusMahasiswa == "none" || statusMahasiswa == "menunggu";

            if (isInProgress && !isExpired)
                return 0;

            if (isNotStartedByStudent && !isExpired)
                return 1;

            if (isWaitingStart)
                return 2;

            if (isFinishedByStudent)
                return 3;

            if (isExpired)
                return 4;

            return 5;
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            if (RefreshButton.IsEnabled)
            {
                RefreshButton.IsEnabled = false;
                RefreshButton.Content = "Memuat...";

                await LoadExams();

                await Task.Delay(2000);
                RefreshButton.IsEnabled = true;
                RefreshButton.Content = "🔄 Refresh";
            }
        }
    }
}
