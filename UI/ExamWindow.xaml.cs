using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using CBTSecureDesktop.Security;
using CBTSecureDesktop.Services;

namespace CBTSecureDesktop.UI
{
    public partial class ExamWindow : Window
    {
        private readonly AuthService _authService;
        private readonly ExamService _examService;
        private readonly long _ujianId;
        private readonly long _mahasiswaId;
        private List<ExamQuestion> _questions = new();
        private int _currentQuestionIndex = 0;

        // Security components
        private readonly KioskManager _kioskManager;
        private readonly KeyboardHookService _keyboardHook;
        private readonly ProcessMonitorService _processMonitor;

        // Timer
        private readonly DispatcherTimer _timer;
        private int _elapsedSeconds = 0;

        public ExamWindow(AuthService authService, ExamService examService, long ujianId, long mahasiswaId)
        {
            InitializeComponent();

            _authService = authService;
            _examService = examService;
            _ujianId = ujianId;
            _mahasiswaId = mahasiswaId;

            // Initialize security components
            _kioskManager = new KioskManager();
            _keyboardHook = new KeyboardHookService();
            _processMonitor = new ProcessMonitorService();

            // Initialize timer
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += Timer_Tick;

            Loaded += ExamWindow_Loaded;
            Closing += ExamWindow_Closing;
        }

        private async void ExamWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Load exam questions from database
                _questions = await _examService.LoadExamQuestionsAsync(_ujianId, _mahasiswaId);

                if (_questions.Count == 0)
                {
                    MessageBox.Show("No questions found for this exam.", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    this.Close();
                    return;
                }

                // ACTIVATE SECURITY MODE
                ActivateSecurityMode();

                // Display first question
                DisplayQuestion(0);

                // Build Navigation Panel
                GenerateNavPanelButtons();

                // Start timer
                _timer.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load exam: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                this.Close();
            }
        }

        /// <summary>
        /// Activates both Kiosk Mode and Keyboard Hook for secure exam environment.
        /// </summary>
        private void ActivateSecurityMode()
        {
            try
            {
                // Enable Kiosk Mode - fullscreen, hide taskbar
                _kioskManager.EnableKioskMode(this);

                // Start Keyboard Hook - block system shortcuts
                _keyboardHook.StartHook();

                // Start Process Monitor
                _processMonitor.StartMonitoring(processName => 
                {
                    Dispatcher.Invoke(() => 
                    {
                        MessageBox.Show($"Peringatan Keamanan!\n\nAplikasi terlarang '{processName}' terdeteksi dan telah dihentikan oleh sistem.", 
                            "Pelanggaran Keamanan", MessageBoxButton.OK, MessageBoxImage.Warning);
                    });
                });

                System.Diagnostics.Debug.WriteLine("✓ Security Mode Activated");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Warning: Could not fully activate security mode.\n{ex.Message}",
                    "Security Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// Deactivates security features when exam ends.
        /// </summary>
        private void DeactivateSecurityMode()
        {
            try
            {
                // Stop process monitor
                _processMonitor.StopMonitoring();

                // Stop keyboard hook
                _keyboardHook.StopHook();

                // Disable kiosk mode
                _kioskManager.DisableKioskMode();

                System.Diagnostics.Debug.WriteLine("✓ Security Mode Deactivated");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deactivating security: {ex.Message}");
            }
        }

        private void DisplayQuestion(int index)
        {
            if (index < 0 || index >= _questions.Count)
                return;

            _currentQuestionIndex = index;
            var question = _questions[index];

            // Update question display
            QuestionNumberText.Text = $"Pertanyaan {question.QuestionNumber}";
            QuestionText.Text = question.QuestionText;
            QuestionCountText.Text = $"Soal {index + 1} dari {_questions.Count}";

            // Update progress
            int answeredCount = _questions.Count(q => q.SelectedAnswer.HasValue);
            ProgressText.Text = $"Progress: {answeredCount}/{_questions.Count} Dijawab";

            // Clear and rebuild options
            OptionsPanel.Children.Clear();

            for (int i = 0; i < question.Options.Count; i++)
            {
                var radioButton = new RadioButton
                {
                    Content = question.Options[i],
                    Tag = i,
                    FontSize = 16,
                    Margin = new Thickness(0),
                    Padding = new Thickness(15),
                    GroupName = "QuestionOptions",
                    IsChecked = question.SelectedAnswer == i,
                    Foreground = new SolidColorBrush(Color.FromRgb(51, 51, 51)), // PolinemaDarkGray
                    FontWeight = FontWeights.Medium
                };

                radioButton.Checked += OptionRadioButton_Checked;

                // Style the radio button with Polinema colors
                var border = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(255, 255, 255)), // White
                    BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224)), // Border gray
                    BorderThickness = new Thickness(2),
                    CornerRadius = new CornerRadius(10),
                    Padding = new Thickness(20, 15, 20, 15),
                    Margin = new Thickness(0, 0, 0, 12),
                    Child = radioButton
                };

                // Add hover effect
                border.MouseEnter += (s, args) =>
                {
                    if (radioButton.IsChecked != true)
                    {
                        border.BorderBrush = new SolidColorBrush(Color.FromRgb(30, 58, 138)); // PolinemaBluePrimary
                        border.Background = new SolidColorBrush(Color.FromRgb(245, 245, 245)); // Light gray
                    }
                };

                border.MouseLeave += (s, args) =>
                {
                    if (radioButton.IsChecked != true)
                    {
                        border.BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224));
                        border.Background = new SolidColorBrush(Color.FromRgb(255, 255, 255));
                    }
                };

                // Style when selected
                if (radioButton.IsChecked == true)
                {
                    border.Background = new SolidColorBrush(Color.FromArgb(30, 30, 58, 138)); // Light blue tint
                    border.BorderBrush = new SolidColorBrush(Color.FromRgb(34, 197, 94)); // PolinemaGreen
                    border.BorderThickness = new Thickness(3);
                }

                radioButton.Checked += (s, args) =>
                {
                    // Reset all borders
                    foreach (var child in OptionsPanel.Children)
                    {
                        if (child is Border b)
                        {
                            b.Background = new SolidColorBrush(Color.FromRgb(255, 255, 255));
                            b.BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224));
                            b.BorderThickness = new Thickness(2);
                        }
                    }
                    // Highlight selected
                    border.Background = new SolidColorBrush(Color.FromArgb(30, 30, 58, 138));
                    border.BorderBrush = new SolidColorBrush(Color.FromRgb(34, 197, 94));
                    border.BorderThickness = new Thickness(3);
                };

                OptionsPanel.Children.Add(border);
            }

            // Update navigation buttons
            PreviousButton.Visibility = index > 0 ? Visibility.Visible : Visibility.Collapsed;
            NextButton.Visibility = index < _questions.Count - 1 ? Visibility.Visible : Visibility.Collapsed;

            UpdateNavPanelHighlight();
        }

        private async void OptionRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton radioButton && radioButton.Tag is int answerIndex)
            {
                // Save answer locally
                _questions[_currentQuestionIndex].SelectedAnswer = answerIndex;

                // Update progress
                int answeredCount = _questions.Count(q => q.SelectedAnswer.HasValue);
                ProgressText.Text = $"Progress: {answeredCount}/{_questions.Count} Dijawab";

                // Update nav panel
                UpdateNavPanelHighlight();

                // Save answer to database
                await _examService.SaveAnswerAsync(_questions[_currentQuestionIndex].QuestionNumber, answerIndex);
            }
        }

        private void PreviousButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentQuestionIndex > 0)
            {
                DisplayQuestion(_currentQuestionIndex - 1);
            }
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentQuestionIndex < _questions.Count - 1)
            {
                DisplayQuestion(_currentQuestionIndex + 1);
            }
        }

        private void ToggleNavButton_Click(object sender, RoutedEventArgs e)
        {
            NavPanel.Visibility = NavPanel.Visibility == Visibility.Collapsed 
                ? Visibility.Visible 
                : Visibility.Collapsed;
        }

        private void GenerateNavPanelButtons()
        {
            QuestionNavPanel.Children.Clear();
            for (int i = 0; i < _questions.Count; i++)
            {
                var btn = new Button
                {
                    Content = (i + 1).ToString(),
                    Width = 45,
                    Height = 45,
                    Margin = new Thickness(5),
                    Tag = i,
                    Cursor = System.Windows.Input.Cursors.Hand,
                    FontSize = 14,
                    FontWeight = FontWeights.Bold
                };

                btn.Click += NavPanelButton_Click;
                QuestionNavPanel.Children.Add(btn);
            }
            UpdateNavPanelHighlight();
        }

        private void NavPanelButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int index)
            {
                DisplayQuestion(index);
            }
        }

        private async void RefreshExamButton_Click(object sender, RoutedEventArgs e)
        {
            if (RefreshExamButton.IsEnabled)
            {
                RefreshExamButton.IsEnabled = false;
                RefreshExamButton.Content = "Menyegarkan...";

                try
                {
                    // Remember current answers state
                    var currentAnswers = _questions.Select(q => q.SelectedAnswer).ToArray();
                    int lastIndex = _currentQuestionIndex;

                    // Reload questions
                    _questions = await _examService.LoadExamQuestionsAsync(_ujianId, _mahasiswaId);

                    if (_questions.Count > 0)
                    {
                        // Safely reapply local transient state if db miss
                        for(int i = 0; i < currentAnswers.Length && i < _questions.Count; i++)
                        {
                            if (!_questions[i].SelectedAnswer.HasValue && currentAnswers[i].HasValue)
                            {
                                _questions[i].SelectedAnswer = currentAnswers[i];
                            }
                        }

                        // Rebind UI
                        GenerateNavPanelButtons();
                        DisplayQuestion(lastIndex < _questions.Count ? lastIndex : 0);
                    }
                }
                catch
                {
                    MessageBox.Show("Gagal menyegarkan soal. Pastikan koneksi internet Anda stabil.", "Kesalahan Jaringan", MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                // Temporary block (10s) to prevent spamming server
                await Task.Delay(10000);

                if (RefreshExamButton != null) // Avoid crash if closed
                {
                    RefreshExamButton.IsEnabled = true;
                    RefreshExamButton.Content = "🔄 Segarkan Soal";
                }
            }
        }

        private void UpdateNavPanelHighlight()
        {
            if (QuestionNavPanel == null) return;

            for (int i = 0; i < QuestionNavPanel.Children.Count; i++)
            {
                if (QuestionNavPanel.Children[i] is Button btn)
                {
                    bool isAnswered = _questions[i].SelectedAnswer.HasValue;
                    bool isCurrent = i == _currentQuestionIndex;

                    if (isCurrent)
                    {
                        btn.Background = new SolidColorBrush(Color.FromRgb(251, 191, 36)); // PolinemaYellow
                        btn.Foreground = new SolidColorBrush(Color.FromRgb(30, 58, 138)); // PolinemaBluePrimary
                        btn.BorderBrush = new SolidColorBrush(Color.FromRgb(30, 58, 138));
                        btn.BorderThickness = new Thickness(2);
                    }
                    else if (isAnswered)
                    {
                        btn.Background = new SolidColorBrush(Color.FromRgb(34, 197, 94)); // PolinemaGreen
                        btn.Foreground = new SolidColorBrush(Color.FromRgb(255, 255, 255)); // White
                        btn.BorderThickness = new Thickness(0);
                    }
                    else
                    {
                        btn.Background = new SolidColorBrush(Color.FromRgb(245, 245, 245)); // LightGray
                        btn.Foreground = new SolidColorBrush(Color.FromRgb(51, 51, 51)); // DarkGray
                        btn.BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224));
                        btn.BorderThickness = new Thickness(1);
                    }
                }
            }
        }

        private async void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            // Check if all questions are answered
            var unanswered = _questions.Count(q => q.SelectedAnswer == null);

            if (unanswered > 0)
            {
                var result = MessageBox.Show(
                    $"Anda memiliki {unanswered} soal yang belum dijawab.\n\n" +
                    "Apakah Anda tetap ingin mengirim ujian?",
                    "Konfirmasi Pengiriman",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.No)
                    return;
            }
            else
            {
                var result = MessageBox.Show(
                    "Apakah Anda yakin ingin mengirim jawaban ujian?\n\n" +
                    "Anda tidak dapat mengubah jawaban setelah dikirim.",
                    "Konfirmasi Pengiriman",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.No)
                    return;
            }

            // Stop timer
            _timer.Stop();

            // Disable buttons
            SubmitButton.IsEnabled = false;
            PreviousButton.IsEnabled = false;
            NextButton.IsEnabled = false;

            try
            {
                // Submit exam to database
                bool success = await _examService.SubmitExamAsync(_ujianId, _mahasiswaId);

                if (success)
                {
                    // Get exam result
                    var result = await _examService.GetExamResultAsync(_ujianId, _mahasiswaId);

                    string message = "Ujian berhasil dikirim!\n\n" +
                                   $"Waktu pengerjaan: {_elapsedSeconds / 60} menit {_elapsedSeconds % 60} detik";

                    if (result != null && result.Nilai.HasValue)
                    {
                        message += $"\n\n📊 Nilai Anda: {result.Nilai.Value}%";
                    }

                    MessageBox.Show(message, "Pengiriman Selesai",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    // Deactivate security and navigate to dashboard
                    DeactivateSecurityMode();
                    var dashboard = new DashboardWindow(_authService);
                    dashboard.Show();
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Gagal mengirim ujian. Silakan coba lagi.",
                        "Kesalahan Pengiriman", MessageBoxButton.OK, MessageBoxImage.Error);

                    // Re-enable buttons
                    SubmitButton.IsEnabled = true;
                    _timer.Start();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Kesalahan mengirim ujian: {ex.Message}",
                    "Kesalahan", MessageBoxButton.OK, MessageBoxImage.Error);

                SubmitButton.IsEnabled = true;
                _timer.Start();
            }
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            _elapsedSeconds++;
            int minutes = _elapsedSeconds / 60;
            int seconds = _elapsedSeconds % 60;
            TimerText.Text = $"Time: {minutes:D2}:{seconds:D2}";
        }

        private void ExamWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            // Prevent accidental closing during exam
            if (_kioskManager.IsKioskModeActive)
            {
                var result = MessageBox.Show(
                    "You cannot close the exam window without submitting.\n\n" +
                    "Do you want to submit your exam now?",
                    "Cannot Close",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    // User wants to submit
                    e.Cancel = true;
                    SubmitButton_Click(this, new RoutedEventArgs());
                }
                else
                {
                    // Cancel the close
                    e.Cancel = true;
                }
            }
            else
            {
                // Security already deactivated (after submission), allow close
                _timer.Stop();
                DeactivateSecurityMode();
            }
        }
    }
}
