using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using CBTSecureDesktop.Security;
using CBTSecureDesktop.Services;
using CBTSecureDesktop.Helpers;
using ManagedNativeWifi;

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

        // Image Service
        private readonly ImageService _imageService = new ImageService();
        private const double MinImageZoom = 0.5;
        private const double MaxImageZoom = 3.0;
        private const double ImageZoomStep = 0.1;
        private double _currentImageZoom = 1.0;
        private int _imageRefreshStatusToken = 0;

        // Cached values to compute remaining time efficiently
        private DateTime? _examGlobalEndTime = null;
        private int _examGlobalExtendMinutes = 0;

        // Network status tracking
        private bool _isOnline = true;

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

            _examService.PendingCountChanged += pendingCount =>
            {
                Task pendingRefresh = RefreshPendingCountAsync();
            };

            // State-Reconciliation: react to session-takeover breach detected during answer saves
            _examService.SecurityBreachDetected += OnSecurityBreachDetected;

            // Initialize pending count immediately for this exam session
            Task initialPendingRefresh = RefreshPendingCountAsync();

            // Start immediate network monitoring to keep UI responsive
            // This loop runs independently and continuously checks network status
            _ = Task.Run(async () =>
            {
                bool lastOnline = true;
                while (true)
                {
                    try
                    {
                        // Quick network check (short timeout)
                        var online = await _examService.TestConnectionAsync();

                        // Always update _isOnline status (don't skip unchanged state)
                        _isOnline = online;

                        // Only update UI when status changes
                        if (online != lastOnline)
                        {
                            Application.Current.Dispatcher.Invoke(() => 
                            {
                                UpdateOfflineUIStatus(online);
                                if (online)
                                {
                                    PendingStatusText.Text = "Mengirim ke server...";
                                    // Trigger flush when reconnected
                                    _ = _examService.TriggerFlushAsync();
                                }
                            });
                            lastOnline = online;
                        }
                    }
                    catch
                    {
                        // If check fails, assume offline
                        _isOnline = false;
                        if (lastOnline)
                        {
                            Application.Current.Dispatcher.Invoke(() => 
                            {
                                UpdateOfflineUIStatus(false);
                            });
                            lastOnline = false;
                        }
                    }

                    // Check more frequently for better responsiveness (2 seconds instead of 5)
                    await Task.Delay(2000);
                }
            });

            Loaded += ExamWindow_Loaded;
            Closing += ExamWindow_Closing;
        }

        private async Task RefreshPendingCountAsync()
        {
            try
            {
                var count = await _examService.GetPendingCountAsync(_ujianId, _mahasiswaId);
                await Dispatcher.InvokeAsync(() =>
                {
                    PendingCountText.Text = count.ToString();
                    PendingCountBorder.Visibility = count > 0 ? Visibility.Visible : Visibility.Collapsed;
                    UpdatePendingStatusText(count);
                });
            }
            catch
            {
            }
        }

        private async void ExamWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Disable window interaction during loading
                this.IsEnabled = false;

                // Load exam questions from database
                _questions = await _examService.LoadExamQuestionsAsync(_ujianId, _mahasiswaId);

                if (_questions.Count == 0)
                {
                    this.IsEnabled = true;
                    MessageBox.Show("No questions found for this exam.", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    this.Close();
                    return;
                }

                // Pre-download all images
                await PreloadImagesAsync();

                // ACTIVATE SECURITY MODE
                ActivateSecurityMode();

                // Display first question
                DisplayQuestion(0);

                // Build Navigation Panel
                GenerateNavPanelButtons();

                // Start timer and refresh clock

                    // Attempt to read global exam end time once to avoid repeated DB queries
                    try
                    {
                        var exams = await _examService.GetAvailableExamsAsync(_mahasiswaId);
                        var ujianInfo = exams.FirstOrDefault(u => u.UjianId == _ujianId);
                        if (ujianInfo != null)
                        {
                            _examGlobalEndTime = ujianInfo.EndTime;
                            _examGlobalExtendMinutes = ujianInfo.ExtendTimeMinutes;
                        }
                    }
                    catch { }

                    _timer.Start();
                    UpdateClockAndRemainingTime();

                    this.IsEnabled = true;
            }
            catch (Exception ex)
            {
                this.IsEnabled = true;
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

        private void UpdateClockAndRemainingTime()
        {
            try
            {
                var now = DateTime.Now;
                ClockText.Text = now.ToString("HH:mm:ss");

                // Query the global exam record (t_ujian) for authoritative end time
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var ujian = await _examService.GetExamByIdAsync(_ujianId, _mahasiswaId);
                        DateTime? endTime = ujian?.EndTime ?? _examGlobalEndTime;
                        int extendMinutes = ujian?.ExtendTimeMinutes ?? _examGlobalExtendMinutes;

                        if (!endTime.HasValue || endTime == DateTime.MinValue)
                        {
                            Application.Current.Dispatcher.Invoke(() => TimerText.Text = "--:--:--");
                            return;
                        }

                        DateTime finalEnd = endTime.Value.AddMinutes(extendMinutes);

                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            var remaining = finalEnd - DateTime.Now;

                            if (remaining <= TimeSpan.Zero)
                            {
                                TimerText.Text = "00:00:00";
                            }
                            else
                            {
                                var total = (long)remaining.TotalSeconds;
                                var hours = total / 3600;
                                var minutes = (total % 3600) / 60;
                                var seconds = total % 60;
                                TimerText.Text = $"{hours:D2}:{minutes:D2}:{seconds:D2}";
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error updating remaining time: {ex.Message}");
                        Application.Current.Dispatcher.Invoke(() => TimerText.Text = "--:--:--");
                    }
                });
            }
            catch { }
        }

        /// <summary>
        /// Asynchronously pre-downloads all images needed for the exam before the first question is displayed.
        /// </summary>
        private async Task PreloadImagesAsync()
        {
            if (_questions == null || _questions.Count == 0) return;

            // Hanya unduh gambar untuk soal pertama secara sinkron agar segera tampil,
            // sisa gambar akan diunduh oleh proses background (Background Download)
            var firstQuestion = _questions[0];
            if (firstQuestion.Images != null && firstQuestion.Images.Count > 0 && !string.IsNullOrEmpty(firstQuestion.Images[0]))
            {
                try
                {
                    await _imageService.GetImageAsync(firstQuestion.Images[0], "abc123");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to preload image ID {firstQuestion.Images[0]}: {ex.Message}");
                }
            }
        }

        private async void DisplayQuestion(int index)
        {
            if (index < 0 || index >= _questions.Count)
                return;

            _currentQuestionIndex = index;
            var question = _questions[index];
            _currentImageZoom = 1.0;

            // Update question display
            QuestionNumberText.Text = $"Pertanyaan {question.QuestionNumber}";
            HtmlHelper.ApplyFormattedHtml(QuestionText, question.QuestionText);
            QuestionCountText.Text = $"Soal {index + 1} dari {_questions.Count}";
            UpdateDoubtToggleButtonState();

            ImageRefreshStatusText.Text = string.Empty;
            ImageRefreshStatusText.Visibility = Visibility.Collapsed;

            // Manage image display (Image caching logic moved to PreDownloadImagesAsync)
            if (question.Images != null && question.Images.Count > 0 && !string.IsNullOrEmpty(question.Images[0]))
            {
                try
                {
                    SetImageSectionVisibility(Visibility.Visible);
                    QuestionImage.Visibility = Visibility.Visible;

                    // The image might be locally cached already via PreDownloadImagesAsync. 
                    // To prevent UI blocking repeatedly, we still use GetImageAsync which loads from Cache.
                    var bitmap = await _imageService.GetImageAsync(question.Images[0], "abc123");
                    QuestionImage.Source = bitmap;
                    ApplyImageZoom();
                }
                catch
                {
                    SetImageSectionVisibility(Visibility.Collapsed);
                    QuestionImage.Visibility = Visibility.Collapsed;
                    QuestionImage.Source = null;
                    ApplyImageZoom();
                }
            }
            else
            {
                SetImageSectionVisibility(Visibility.Collapsed);
                QuestionImage.Visibility = Visibility.Collapsed;
                QuestionImage.Source = null;
                ApplyImageZoom();
            }

            // Update progress
            UpdateProgressText();

            // Clear and rebuild options
            OptionsPanel.Children.Clear();

            if (question.IsMultiAnswer)
            {
                var infoText = new TextBlock
                {
                    Text = "* Soal ini memiliki lebih dari satu jawaban benar. Ketuk opsi kembali untuk membatalkan pilihan.",
                    Foreground = new SolidColorBrush(Color.FromRgb(220, 38, 38)), // Red/Warning
                    FontStyle = FontStyles.Italic,
                    Margin = new Thickness(0, 0, 0, 15),
                    TextWrapping = TextWrapping.Wrap
                };
                OptionsPanel.Children.Add(infoText);
            }

            for (int i = 0; i < question.Options.Count; i++)
            {
                System.Windows.Controls.Primitives.ToggleButton inputControl;
                int currentIndex = i;

                var optionHtml = question.Options[currentIndex];
                var optionFile = currentIndex < question.OptionFiles.Count ? question.OptionFiles[currentIndex] : null;

                var radioButton = new RadioButton
                {
                    Tag = currentIndex,
                    FontSize = 16,
                    Margin = new Thickness(0),
                    Padding = new Thickness(15),
                    Foreground = new SolidColorBrush(Color.FromRgb(51, 51, 51)), // PolinemaDarkGray
                    FontWeight = FontWeights.Medium,
                    Content = await BuildOptionContentAsync(optionHtml, optionFile, question.OptionIds[currentIndex])
                };

                if (question.IsMultiAnswer)
                {
                    radioButton.GroupName = $"MultiOption_{currentIndex}_{Guid.NewGuid()}";
                    radioButton.IsChecked = question.SelectedAnswers.Contains(currentIndex);

                    radioButton.PreviewMouseLeftButtonDown += (s, e) =>
                    {
                        if (radioButton.IsChecked == true)
                        {
                            radioButton.IsChecked = false;
                            e.Handled = true; // Prevent internal WPF RadioButton handling which avoids re-checking
                        }
                    };

                    radioButton.Checked += OptionMultiRadioButton_Checked;
                    radioButton.Unchecked += OptionMultiRadioButton_Unchecked;
                }
                else
                {
                    radioButton.GroupName = "QuestionOptions";
                    radioButton.IsChecked = question.SelectedAnswer == currentIndex;
                    radioButton.Checked += OptionRadioButton_Checked;
                }

                inputControl = radioButton;

                // Style the control with Polinema colors (wrap in Border)
                var border = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(255, 255, 255)), // White
                    BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224)), // Border gray
                    BorderThickness = new Thickness(2),
                    CornerRadius = new CornerRadius(10),
                    Padding = new Thickness(20, 15, 20, 15),
                    Margin = new Thickness(0, 0, 0, 12),
                    Child = inputControl
                };

                // Add hover effect
                border.MouseEnter += (s, args) =>
                {
                    if (inputControl.IsChecked != true)
                    {
                        border.BorderBrush = new SolidColorBrush(Color.FromRgb(30, 58, 138)); // PolinemaBluePrimary
                        border.Background = new SolidColorBrush(Color.FromRgb(245, 245, 245)); // Light gray
                    }
                };

                border.MouseLeave += (s, args) =>
                {
                    if (inputControl.IsChecked != true)
                    {
                        border.BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224));
                        border.Background = new SolidColorBrush(Color.FromRgb(255, 255, 255));
                    }
                };

                // Style when selected initially
                if (inputControl.IsChecked == true)
                {
                    border.Background = new SolidColorBrush(Color.FromArgb(30, 30, 58, 138)); // Light blue tint
                    border.BorderBrush = new SolidColorBrush(Color.FromRgb(34, 197, 94)); // PolinemaGreen
                    border.BorderThickness = new Thickness(3);
                }

                if (!question.IsMultiAnswer)
                {
                    inputControl.Checked += (s, args) =>
                    {
                        // Reset all borders for single choice
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
                }
                else
                {
                    inputControl.Checked += (s, args) =>
                    {
                        border.Background = new SolidColorBrush(Color.FromArgb(30, 30, 58, 138));
                        border.BorderBrush = new SolidColorBrush(Color.FromRgb(34, 197, 94));
                        border.BorderThickness = new Thickness(3);
                    };
                    inputControl.Unchecked += (s, args) =>
                    {
                        border.Background = new SolidColorBrush(Color.FromRgb(255, 255, 255));
                        border.BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224));
                        border.BorderThickness = new Thickness(2);
                    };
                }

                OptionsPanel.Children.Add(border);
            }

            // Update navigation buttons
            PreviousButton.Visibility = index > 0 ? Visibility.Visible : Visibility.Collapsed;
            NextButton.Visibility = index < _questions.Count - 1 ? Visibility.Visible : Visibility.Collapsed;

            UpdateNavPanelHighlight();
        }

        private void OptionMultiRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton radioButton && radioButton.Tag is int answerIndex)
            {
                var question = _questions[_currentQuestionIndex];
                if (!question.SelectedAnswers.Contains(answerIndex))
                {
                    question.SelectedAnswers.Add(answerIndex);
                }

                UpdateProgressAndSaveMulti();
            }
        }

        private void OptionMultiRadioButton_Unchecked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton radioButton && radioButton.Tag is int answerIndex)
            {
                var question = _questions[_currentQuestionIndex];
                if (question.SelectedAnswers.Contains(answerIndex))
                {
                    question.SelectedAnswers.Remove(answerIndex);
                }

                UpdateProgressAndSaveMulti();
            }
        }

        private async Task<object> BuildOptionContentAsync(string optionHtml, string? optionFile, long optionId)
        {
            if (optionId > 0 && !string.IsNullOrWhiteSpace(optionFile))
            {
                try
                {
                    var optionImage = await _imageService.GetOptionImageAsync((int)optionId, "abc123");
                    var image = new Image
                    {
                        Source = optionImage,
                        Stretch = Stretch.Uniform,
                        MaxHeight = 180,
                        HorizontalAlignment = HorizontalAlignment.Left,
                        Margin = new Thickness(0, 0, 0, 8),
                        Cursor = Cursors.Hand,
                        ToolTip = "Klik gambar untuk memperbesar"
                    };

                    image.PreviewMouseLeftButtonDown += OptionImage_PreviewMouseLeftButtonDown;

                    if (string.IsNullOrWhiteSpace(optionHtml))
                    {
                        return image;
                    }

                    return new StackPanel
                    {
                        Children =
                        {
                            image,
                            HtmlHelper.CreateFormattedTextBlock(optionHtml, new SolidColorBrush(Color.FromRgb(51, 51, 51)), 16, TextWrapping.Wrap)
                        }
                    };
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to load option image {optionId}: {ex.Message}");
                }
            }

            return HtmlHelper.CreateFormattedTextBlock(optionHtml, new SolidColorBrush(Color.FromRgb(51, 51, 51)), 16, TextWrapping.Wrap);
        }

        private void OptionImage_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;

            if (sender is Image image && image.Source is not null)
            {
                ShowOptionImagePopup(image.Source);
            }
        }

        private void ShowOptionImagePopup(ImageSource imageSource)
        {
            var popupWindow = new Window
            {
                Title = "Preview Gambar Opsi",
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Width = 900,
                Height = 700,
                MinWidth = 600,
                MinHeight = 450,
                Background = new SolidColorBrush(Color.FromRgb(15, 23, 42))
            };

            var root = new Grid
            {
                Margin = new Thickness(14)
            };
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var image = new Image
            {
                Source = imageSource,
                Stretch = Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            var scrollViewer = new ScrollViewer
            {
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Content = image,
                Background = new SolidColorBrush(Color.FromRgb(15, 23, 42))
            };

            Grid.SetRow(scrollViewer, 0);
            root.Children.Add(scrollViewer);

            var closeButton = new Button
            {
                Content = "Tutup",
                Width = 100,
                Margin = new Thickness(0, 12, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Right,
                Style = (Style)FindResource("PolinemaButtonSecondary")
            };
            closeButton.Click += (_, _) => popupWindow.Close();

            Grid.SetRow(closeButton, 1);
            root.Children.Add(closeButton);

            popupWindow.Content = root;
            popupWindow.ShowDialog();
        }

        private async void UpdateProgressAndSaveMulti()
        {
            UpdateProgressText();
            UpdateNavPanelHighlight();

            // Save answers silently - errors are queued as pending
            try
            {
                await _examService.SaveAnswersAsync(_questions[_currentQuestionIndex].QuestionNumber, _questions[_currentQuestionIndex].SelectedAnswers);
            }
            catch (Exception ex)
            {
                // Silently handle - error is already queued in service as pending answer
                System.Diagnostics.Debug.WriteLine($"Save multi answers error (already queued): {ex.Message}");
            }
        }

        private async void OptionRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton radioButton && radioButton.Tag is int answerIndex)
            {
                // Save answer locally
                _questions[_currentQuestionIndex].SelectedAnswer = answerIndex;

                // Update progress
                UpdateProgressText();

                // Update nav panel
                UpdateNavPanelHighlight();

                // Save answer to database (quietly - errors become pending)
                try
                {
                    await _examService.SaveAnswerAsync(_questions[_currentQuestionIndex].QuestionNumber, answerIndex);
                }
                catch (Exception ex)
                {
                    // Silently handle - error is already queued in service as pending answer
                    System.Diagnostics.Debug.WriteLine($"Save single answer error (already queued): {ex.Message}");
                }
            }
        }

        private async void RefreshImageButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn)
            {
                return;
            }

            btn.IsEnabled = false;
            btn.Content = "🔄 Menyegarkan...";

            try
            {
                var question = _questions[_currentQuestionIndex];
                if (question.Images == null || question.Images.Count == 0 || string.IsNullOrEmpty(question.Images[0]))
                {
                    await ShowImageRefreshStatusAsync("Tidak ada gambar pada soal ini.", new SolidColorBrush(Color.FromRgb(107, 114, 128)));
                    return;
                }

                var imageId = question.Images[0];
                var bitmap = await _imageService.RefreshImageCacheAsync(imageId, "abc123");
                var downloadFailures = ImageService.ConsumeDownloadFailures();
                var currentImageFailure = downloadFailures.FirstOrDefault(f => f.StartsWith($"{imageId}:"));

                if (!string.IsNullOrEmpty(currentImageFailure))
                {
                    SetImageSectionVisibility(Visibility.Collapsed);
                    QuestionImage.Visibility = Visibility.Collapsed;
                    MessageBox.Show($"Gagal mendownload gambar terbaru.\n\nDetail: {currentImageFailure}", "Refresh Gambar Gagal", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                SetImageSectionVisibility(Visibility.Visible);
                QuestionImage.Source = bitmap;
                QuestionImage.Visibility = Visibility.Visible;
                ApplyImageZoom();

                await ShowImageRefreshStatusAsync("Gambar berhasil diperbarui.", new SolidColorBrush(Color.FromRgb(34, 197, 94)));
            }
            catch (Exception ex)
            {
                SetImageSectionVisibility(Visibility.Collapsed);
                QuestionImage.Visibility = Visibility.Collapsed;
                MessageBox.Show($"Gagal menyegarkan gambar.\n\nDetail: {ex.Message}", "Refresh Gambar Gagal", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            finally
            {
                btn.Content = "🔄 Refresh Gambar";
                btn.IsEnabled = true;
            }
        }

        private void ZoomOutImageButton_Click(object sender, RoutedEventArgs e)
        {
            _currentImageZoom = Math.Max(MinImageZoom, _currentImageZoom - ImageZoomStep);
            ApplyImageZoom();
        }

        private void ZoomInImageButton_Click(object sender, RoutedEventArgs e)
        {
            _currentImageZoom = Math.Min(MaxImageZoom, _currentImageZoom + ImageZoomStep);
            ApplyImageZoom();
        }

        private void ResetZoomImageButton_Click(object sender, RoutedEventArgs e)
        {
            _currentImageZoom = 1.0;
            ApplyImageZoom();
        }

        private void QuestionImage_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (QuestionImage.Visibility != Visibility.Visible)
            {
                return;
            }

            _currentImageZoom = e.Delta > 0
                ? Math.Min(MaxImageZoom, _currentImageZoom + ImageZoomStep)
                : Math.Max(MinImageZoom, _currentImageZoom - ImageZoomStep);

            ApplyImageZoom();
        }

        private void ApplyImageZoom()
        {
            QuestionImage.LayoutTransform = new ScaleTransform(_currentImageZoom, _currentImageZoom);
        }

        private async Task ShowImageRefreshStatusAsync(string message, System.Windows.Media.Brush foreground)
        {
            var statusToken = ++_imageRefreshStatusToken;
            ImageRefreshStatusText.Text = message;
            ImageRefreshStatusText.Foreground = foreground;
            ImageRefreshStatusText.Visibility = Visibility.Visible;

            await Task.Delay(3500);

            if (statusToken == _imageRefreshStatusToken)
            {
                ImageRefreshStatusText.Text = string.Empty;
                ImageRefreshStatusText.Visibility = Visibility.Collapsed;
            }
        }

        private void SetImageSectionVisibility(Visibility visibility)
        {
            if (FindName("ImageSection") is FrameworkElement imageSection)
            {
                imageSection.Visibility = visibility;
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

        private void GenerateNavPanelButtons()
        {
            QuestionNavPanel.Children.Clear();
            for (int i = 0; i < _questions.Count; i++)
            {
                var btn = new Button
                {
                    Content = (i + 1).ToString(),
                    Width = 40,
                    Height = 40,
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

        private static bool IsQuestionAnswered(ExamQuestion question)
        {
            return (!question.IsMultiAnswer && question.SelectedAnswer.HasValue)
                || (question.IsMultiAnswer && question.SelectedAnswers.Count > 0);
        }

        private void UpdateProgressText()
        {
            int answeredCount = _questions.Count(IsQuestionAnswered);
            int doubtfulCount = _questions.Count(q => q.IsDoubtful);
            ProgressText.Text = $"Progress: {answeredCount}/{_questions.Count} Dijawab • Ragu: {doubtfulCount}";
        }

        private void UpdateDoubtToggleButtonState()
        {
            if (DoubtToggleButton == null || _questions.Count == 0 || _currentQuestionIndex < 0 || _currentQuestionIndex >= _questions.Count)
            {
                return;
            }

            bool isDoubtful = _questions[_currentQuestionIndex].IsDoubtful;
            DoubtToggleButton.Content = isDoubtful ? "☑ Ragu-ragu" : "☐ Ragu-ragu";

            if (isDoubtful)
            {
                DoubtToggleButton.Background = new SolidColorBrush(Color.FromRgb(254, 243, 199));
                DoubtToggleButton.Foreground = new SolidColorBrush(Color.FromRgb(146, 64, 14));
                DoubtToggleButton.BorderBrush = new SolidColorBrush(Color.FromRgb(217, 119, 6));
                DoubtToggleButton.BorderThickness = new Thickness(2);
            }
            else
            {
                DoubtToggleButton.Background = new SolidColorBrush(Color.FromRgb(248, 250, 252));
                DoubtToggleButton.Foreground = new SolidColorBrush(Color.FromRgb(30, 58, 138));
                DoubtToggleButton.BorderBrush = new SolidColorBrush(Color.FromRgb(148, 163, 184));
                DoubtToggleButton.BorderThickness = new Thickness(1.5);
            }
        }

        private async void DoubtToggleButton_Click(object sender, RoutedEventArgs e)
        {
            if (_questions.Count == 0 || _currentQuestionIndex < 0 || _currentQuestionIndex >= _questions.Count)
            {
                return;
            }

            var currentQuestion = _questions[_currentQuestionIndex];
            currentQuestion.IsDoubtful = !currentQuestion.IsDoubtful;

            UpdateDoubtToggleButtonState();
            UpdateProgressText();
            UpdateNavPanelHighlight();

            await _examService.SaveDoubtStateAsync(currentQuestion.SoalId, currentQuestion.IsDoubtful);
            await EnsureCurrentQuestionAnswerPersistedAsync(currentQuestion);
        }

        private async Task EnsureCurrentQuestionAnswerPersistedAsync(ExamQuestion question)
        {
            if (question.IsMultiAnswer)
            {
                if (question.SelectedAnswers.Count > 0)
                {
                    await _examService.SaveAnswersAsync(question.QuestionNumber, question.SelectedAnswers.ToList());
                }

                return;
            }

            if (question.SelectedAnswer.HasValue)
            {
                await _examService.SaveAnswerAsync(question.QuestionNumber, question.SelectedAnswer.Value);
            }
        }

        private Dictionary<long, bool> CaptureDoubtStateBySoalId()
        {
            return _questions
                .GroupBy(q => q.SoalId)
                .ToDictionary(g => g.Key, g => g.First().IsDoubtful);
        }

        private static void RestoreDoubtStateBySoalId(Dictionary<long, bool> doubtState, List<ExamQuestion> refreshedQuestions)
        {
            foreach (var question in refreshedQuestions)
            {
                if (doubtState.TryGetValue(question.SoalId, out var isDoubtful))
                {
                    question.IsDoubtful = isDoubtful;
                }
            }
        }

        private Dictionary<long, List<long>> CaptureAnswerStateBySoalId()
        {
            var answerState = new Dictionary<long, List<long>>();

            foreach (var question in _questions)
            {
                if (question.IsMultiAnswer)
                {
                    var selectedOptionIds = question.SelectedAnswers
                        .Where(i => i >= 0 && i < question.OptionIds.Count)
                        .Select(i => question.OptionIds[i])
                        .Distinct()
                        .ToList();

                    if (selectedOptionIds.Count > 0)
                    {
                        answerState[question.SoalId] = selectedOptionIds;
                    }
                }
                else if (question.SelectedAnswer.HasValue && question.SelectedAnswer.Value >= 0 && question.SelectedAnswer.Value < question.OptionIds.Count)
                {
                    answerState[question.SoalId] = new List<long> { question.OptionIds[question.SelectedAnswer.Value] };
                }
            }

            return answerState;
        }

        private static void RestoreAnswerStateBySoalId(Dictionary<long, List<long>> answerState, List<ExamQuestion> refreshedQuestions)
        {
            foreach (var question in refreshedQuestions)
            {
                question.SelectedAnswer = null;
                question.SelectedAnswers.Clear();

                if (!answerState.TryGetValue(question.SoalId, out var selectedOptionIds) || selectedOptionIds.Count == 0)
                {
                    continue;
                }

                var selectedIndices = selectedOptionIds
                    .Select(id => question.OptionIds.IndexOf(id))
                    .Where(index => index >= 0)
                    .Distinct()
                    .ToList();

                if (question.IsMultiAnswer)
                {
                    question.SelectedAnswers.AddRange(selectedIndices);
                }
                else if (selectedIndices.Count > 0)
                {
                    question.SelectedAnswer = selectedIndices[0];
                }
            }
        }

        private async void RefreshExamButton_Click(object sender, RoutedEventArgs e)
        {
            if (!RefreshExamButton.IsEnabled)
            {
                return;
            }

            RefreshExamButton.IsEnabled = false;
            RefreshExamButton.Content = "Menyegarkan...";

            try
            {
                var localAnswerState = CaptureAnswerStateBySoalId();
                var localDoubtState = CaptureDoubtStateBySoalId();
                long? currentSoalId = _questions.Count > 0 && _currentQuestionIndex >= 0 && _currentQuestionIndex < _questions.Count
                    ? _questions[_currentQuestionIndex].SoalId
                    : null;

                var refreshedQuestions = await _examService.GetExamQuestionsAsync(_ujianId);

                if (refreshedQuestions.Count > 0)
                {
                    RestoreAnswerStateBySoalId(localAnswerState, refreshedQuestions);
                    RestoreDoubtStateBySoalId(localDoubtState, refreshedQuestions);
                    _questions = refreshedQuestions;

                    int targetIndex = 0;
                    if (currentSoalId.HasValue)
                    {
                        int sameQuestionIndex = _questions.FindIndex(q => q.SoalId == currentSoalId.Value);
                        if (sameQuestionIndex >= 0)
                        {
                            targetIndex = sameQuestionIndex;
                        }
                    }

                    GenerateNavPanelButtons();
                    DisplayQuestion(targetIndex);
                }
            }
            catch
            {
                MessageBox.Show("Gagal menyegarkan soal. Pastikan koneksi internet Anda stabil.", "Kesalahan Jaringan", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            finally
            {
                await Task.Delay(10000);

                if (RefreshExamButton != null)
                {
                    RefreshExamButton.IsEnabled = true;
                    RefreshExamButton.Content = "🔄 Refresh Soal";
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
                    bool isAnswered = IsQuestionAnswered(_questions[i]);
                    bool isDoubtful = _questions[i].IsDoubtful;
                    bool isCurrent = i == _currentQuestionIndex;

                    if (isCurrent)
                    {
                        btn.Background = new SolidColorBrush(Color.FromRgb(251, 191, 36)); // PolinemaYellow
                        btn.Foreground = new SolidColorBrush(Color.FromRgb(30, 58, 138)); // PolinemaBluePrimary
                        btn.BorderBrush = new SolidColorBrush(Color.FromRgb(30, 58, 138));
                        btn.BorderThickness = new Thickness(2);
                    }
                    else if (isDoubtful)
                    {
                        btn.Background = new SolidColorBrush(Color.FromRgb(249, 115, 22)); // Orange for doubtful
                        btn.Foreground = new SolidColorBrush(Color.FromRgb(255, 255, 255));
                        btn.BorderThickness = new Thickness(0);
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

        private async Task<bool> ShowSubmitPreviewDialog(bool isOnline)
        {
            var answeredConfident = _questions
                .Select((q, index) => new { q, index })
                .Where(x => IsQuestionAnswered(x.q) && !x.q.IsDoubtful)
                .Select(x => x.index)
                .ToList();

            var doubtful = _questions
                .Select((q, index) => new { q, index })
                .Where(x => x.q.IsDoubtful)
                .Select(x => x.index)
                .ToList();

            var unanswered = _questions
                .Select((q, index) => new { q, index })
                .Where(x => !IsQuestionAnswered(x.q))
                .Select(x => x.index)
                .ToList();

            // Get pending soal IDs for offline indicator - in background to avoid blocking
            var pendingSoalIds = await Task.Run(async () => await _examService.GetPendingSoalIdsAsync(_ujianId, _mahasiswaId));
            var pendingQuestionIndexes = _questions
                .Select((q, index) => new { q, index })
                .Where(x => pendingSoalIds.Contains(x.q.SoalId))
                .Select(x => x.index)
                .ToHashSet();

            var dialog = new Window
            {
                Title = "Preview Sebelum Submit",
                Width = 760,
                Height = 620,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                WindowStyle = WindowStyle.ToolWindow,
                ResizeMode = ResizeMode.NoResize,
                Background = new SolidColorBrush(Color.FromRgb(248, 250, 252))
            };

            var root = new Grid { Margin = new Thickness(20) };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var titlePanel = new StackPanel { Margin = new Thickness(0, 0, 0, 14) };
            titlePanel.Children.Add(new TextBlock
            {
                Text = "Ringkasan Jawaban Sebelum Submit",
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(30, 58, 138))
            });
            titlePanel.Children.Add(new TextBlock
            {
                Text = $"Yakin: {answeredConfident.Count} • Ragu-ragu: {doubtful.Count} • Belum dijawab: {unanswered.Count}",
                Margin = new Thickness(0, 6, 0, 0),
                Foreground = new SolidColorBrush(Color.FromRgb(75, 85, 99))
            });
            Grid.SetRow(titlePanel, 0);
            root.Children.Add(titlePanel);

            var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            var sections = new StackPanel();

            StackPanel CreateSection(string sectionTitle, Color headerColor, List<int> indexes, Brush buttonBackground, Brush buttonForeground, Brush buttonBorder)
            {
                var panel = new StackPanel { Margin = new Thickness(0, 0, 0, 14) };

                panel.Children.Add(new TextBlock
                {
                    Text = sectionTitle,
                    FontSize = 15,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(headerColor),
                    Margin = new Thickness(0, 0, 0, 8)
                });

                if (indexes.Count == 0)
                {
                    panel.Children.Add(new TextBlock
                    {
                        Text = "Tidak ada soal pada kategori ini.",
                        Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128)),
                        FontStyle = FontStyles.Italic,
                        Margin = new Thickness(0, 0, 0, 6)
                    });
                    return panel;
                }

                var wrap = new WrapPanel();
                foreach (var index in indexes)
                {
                    var questionNumber = _questions[index].QuestionNumber;
                    var isPending = pendingQuestionIndexes.Contains(index);

                    // Create button with pending indicator
                    var btnGrid = new Grid { Width = 130 };
                    btnGrid.Margin = new Thickness(0, 0, 8, 8);

                    var btn = new Button
                    {
                        Content = $"Soal {questionNumber}",
                        Padding = new Thickness(12, 8, 12, 8),
                        Tag = index,
                        Background = buttonBackground,
                        Foreground = buttonForeground,
                        BorderBrush = buttonBorder,
                        BorderThickness = new Thickness(1),
                        FontWeight = FontWeights.SemiBold,
                        Cursor = Cursors.Hand
                    };

                    btn.Click += (_, __) =>
                    {
                        DisplayQuestion((int)btn.Tag);
                        dialog.DialogResult = false;
                        dialog.Close();
                    };

                    btnGrid.Children.Add(btn);

                    // Add pending badge if applicable
                    if (isPending)
                    {
                        var pendingBadge = new Border
                        {
                            Background = new SolidColorBrush(Color.FromRgb(220, 38, 38)),
                            CornerRadius = new CornerRadius(10),
                            Padding = new Thickness(4, 2, 4, 2),
                            HorizontalAlignment = HorizontalAlignment.Right,
                            VerticalAlignment = VerticalAlignment.Top,
                            Margin = new Thickness(0, -8, -8, 0)
                        };
                        pendingBadge.Child = new TextBlock
                        {
                            Text = "⏱",
                            Foreground = Brushes.White,
                            FontSize = 10,
                            FontWeight = FontWeights.Bold,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center
                        };
                        btnGrid.Children.Add(pendingBadge);
                    }

                    wrap.Children.Add(btnGrid);
                }

                panel.Children.Add(wrap);
                return panel;
            }

            sections.Children.Add(CreateSection(
                "✅ Soal Terjawab Yakin",
                Color.FromRgb(22, 101, 52),
                answeredConfident,
                new SolidColorBrush(Color.FromRgb(34, 197, 94)),
                Brushes.White,
                new SolidColorBrush(Color.FromRgb(22, 101, 52))));

            sections.Children.Add(CreateSection(
                "🤔 Soal Ditandai Ragu-ragu",
                Color.FromRgb(161, 98, 7),
                doubtful,
                new SolidColorBrush(Color.FromRgb(250, 204, 21)),
                new SolidColorBrush(Color.FromRgb(30, 58, 138)),
                new SolidColorBrush(Color.FromRgb(161, 98, 7))));

            sections.Children.Add(CreateSection(
                "❗ Soal Belum Dijawab",
                Color.FromRgb(185, 28, 28),
                unanswered,
                new SolidColorBrush(Color.FromRgb(220, 38, 38)),
                Brushes.White,
                new SolidColorBrush(Color.FromRgb(153, 27, 27))));

            scroll.Content = sections;
            Grid.SetRow(scroll, 1);
            root.Children.Add(scroll);

            // Add offline warning banner if needed
            if (!isOnline)
            {
                var offlineWarningBorder = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(254, 243, 224)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(217, 119, 6)),
                    BorderThickness = new Thickness(0, 0, 0, 2),
                    Padding = new Thickness(16, 12, 16, 12),
                    Margin = new Thickness(0, 0, 0, 14)
                };

                var warningPanel = new StackPanel();
                warningPanel.Children.Add(new TextBlock
                {
                    Text = "⚠️ Mode Offline Aktif",
                    FontSize = 13,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Color.FromRgb(161, 98, 7)),
                    Margin = new Thickness(0, 0, 0, 6)
                });
                warningPanel.Children.Add(new TextBlock
                {
                    Text = "Anda tidak dapat mengirim ujian saat offline. Sambungkan ke internet terlebih dahulu untuk melanjutkan submit.",
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Color.FromRgb(120, 80, 0)),
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 14)
                });

                offlineWarningBorder.Child = warningPanel;

                // Insert warning at top (after row 1)
                root.RowDefinitions.Insert(1, new RowDefinition { Height = GridLength.Auto });
                // Shift scroll to row 2
                Grid.SetRow(scroll, 2);
                offlineWarningBorder.Margin = new Thickness(0, 0, 0, 0);
                Grid.SetRow(offlineWarningBorder, 1);
                root.Children.Add(offlineWarningBorder);
            }

            var footer = new Grid { Margin = new Thickness(0, 10, 0, 0) };
            footer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            footer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            footer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var noteText = new TextBlock
            {
                Text = "Klik soal untuk langsung pindah ke soal tersebut.",
                Foreground = new SolidColorBrush(Color.FromRgb(75, 85, 99)),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(noteText, 0);
            footer.Children.Add(noteText);

            var backButton = new Button
            {
                Content = "Kembali ke Ujian",
                Style = (Style)FindResource("PolinemaButtonSecondary"),
                Padding = new Thickness(14, 9, 14, 9),
                Margin = new Thickness(0, 0, 10, 0)
            };
            backButton.Click += (_, __) =>
            {
                dialog.DialogResult = false;
                dialog.Close();
            };
            Grid.SetColumn(backButton, 1);
            footer.Children.Add(backButton);

            var continueButton = new Button
            {
                Content = "Lanjutkan Submit",
                Style = (Style)FindResource("PolinemaButtonPrimary"),
                Padding = new Thickness(14, 9, 14, 9),
                IsEnabled = isOnline  // Disable if offline
            };
            continueButton.Click += (_, __) =>
            {
                if (!isOnline)
                {
                    MessageBox.Show(
                        "Anda sedang dalam mode offline. Sambungkan ke internet terlebih dahulu untuk mengirim ujian.",
                        "Tidak Bisa Submit - Mode Offline",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }
                dialog.DialogResult = true;
                dialog.Close();
            };
            Grid.SetColumn(continueButton, 2);
            footer.Children.Add(continueButton);

            Grid.SetRow(footer, isOnline ? 2 : 3);  // Adjust row based on whether warning is shown
            root.Children.Add(footer);

            dialog.Content = root;
            return dialog.ShowDialog() == true;
        }

        private async void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            // Disable button immediately to prevent double-clicks/freezing
            var submitBtn = sender as Button;
            if (submitBtn == null) return;

            submitBtn.IsEnabled = false;

            try
            {
                // Call preview dialog asynchronously - pass isOnline parameter
                // User can see preview even offline, but submit will be blocked in dialog
                if (!await ShowSubmitPreviewDialog(_isOnline))
                {
                    submitBtn.IsEnabled = true;
                    return;
                }

                // Check if there are pending answers - use background task
                int pendingCount = await _examService.GetPendingCountAsync(_ujianId, _mahasiswaId);
                if (pendingCount > 0)
                {
                    // Show dialog asking user to sync first
                    var syncResult = MessageBox.Show(
                        $"Anda memiliki {pendingCount} jawaban yang belum sinkronisasi ke server.\n\n" +
                        "Untuk memastikan semua jawaban terekam dengan baik, silakan:\n" +
                        "1. Pastikan koneksi internet Anda stabil\n" +
                        "2. Tunggu hingga semua jawaban tersinkronisasi\n\n" +
                        "Lanjutkan submit sekarang? (Jawaban yang belum tersinkronisasi akan tetap dicoba dikirim)",
                        "Jawaban Belum Tersinkronisasi",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);

                    if (syncResult == MessageBoxResult.No)
                    {
                        submitBtn.IsEnabled = true;
                        return;
                    }

                    // Show sync progress
                    await ShowSyncProgressAsync(pendingCount);
                }

                var unanswered = _questions.Count(q => !IsQuestionAnswered(q));
                var doubtful = _questions.Count(q => q.IsDoubtful);

                var result = MessageBox.Show(
                    $"Ringkasan sebelum submit:\n- Belum dijawab: {unanswered}\n- Ditandai ragu-ragu: {doubtful}\n\n" +
                    "Apakah Anda yakin ingin mengirim jawaban ujian?\nAnda tidak dapat mengubah jawaban setelah dikirim.",
                    "Konfirmasi Pengiriman",
                    MessageBoxButton.YesNo,
                    unanswered > 0 || doubtful > 0 ? MessageBoxImage.Question : MessageBoxImage.Warning);

                if (result == MessageBoxResult.No)
                {
                    submitBtn.IsEnabled = true;
                    return;
                }

                // Stop timer
                _timer.Stop();

                // Disable nav buttons
                PreviousButton.IsEnabled = false;
                NextButton.IsEnabled = false;

                try
                {
                    // Submit exam to database in background (non-blocking)
                    bool success = await Task.Run(async () => await _examService.SubmitExamAsync(_ujianId, _mahasiswaId));

                    if (success)
                    {
                        await _examService.ClearDoubtStatesAsync(_ujianId, _mahasiswaId);

                        // Keep the feedback minimal: only show that exam is finished
                        string message = "Ujian berhasil dikirim!\n\nStatus: Selesai." +
                                         $"\nWaktu pengerjaan: {_elapsedSeconds / 60} menit {_elapsedSeconds % 60} detik";

                        MessageBox.Show(message, "Pengiriman Selesai",
                            MessageBoxButton.OK, MessageBoxImage.Information);

                        // Delete any downloaded cached images related to this exam
                        try
                        {
                            var imageIds = _questions
                                .SelectMany(q => q.Images ?? Enumerable.Empty<string>())
                                .Where(id => !string.IsNullOrWhiteSpace(id))
                                .Distinct()
                                .ToList();

                            if (imageIds.Count > 0)
                            {
                                // Fire-and-forget deletion to speed up returning to dashboard.
                                _ = _imageService.DeleteCacheFilesAsync(imageIds);
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Failed to delete cached images after submit: {ex.Message}");
                        }

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
                        submitBtn.IsEnabled = true;
                        PreviousButton.IsEnabled = true;
                        NextButton.IsEnabled = true;
                        _timer.Start();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Kesalahan mengirim ujian: {ex.Message}",
                        "Kesalahan", MessageBoxButton.OK, MessageBoxImage.Error);

                    submitBtn.IsEnabled = true;
                    PreviousButton.IsEnabled = true;
                    NextButton.IsEnabled = true;
                    _timer.Start();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Submit button error: {ex.Message}");
                submitBtn.IsEnabled = true;
            }
        }

        private async void Timer_Tick(object? sender, EventArgs e)
        {
            _elapsedSeconds++;
            UpdateClockAndRemainingTime();

            // Periksa status ujian setiap 10 detik
            if (_elapsedSeconds % 10 == 0)
            {
                string? status = await _examService.GetExamStatusAsync(_ujianId, _mahasiswaId);
                if (status == "dihentikan")
                {
                    _timer.Stop();
                    SubmitButton.IsEnabled = false;
                    this.IsEnabled = false; // block user interaction immediately

                    // Secara siluman kumpulkan poinnya ke database dengan status dihentikan
                    await _examService.CalculateAndSaveForceStopScoreAsync(_ujianId, _mahasiswaId);
                    await _examService.ClearDoubtStatesAsync(_ujianId, _mahasiswaId);

                    MessageBox.Show("Ujian Anda telah dihentikan secara paksa oleh Admin/Pengawas.\n\n" +
                                    "Segala jawaban yang telah terisi telah dikumpulkan dan diakumulasikan.",
                        "Ujian Dihentikan", MessageBoxButton.OK, MessageBoxImage.Warning);

                    // Delete any downloaded cached images related to this exam
                    try
                    {
                        var imageIds = _questions
                            .SelectMany(q => q.Images ?? Enumerable.Empty<string>())
                            .Where(id => !string.IsNullOrWhiteSpace(id))
                            .Distinct()
                            .ToList();

                        if (imageIds.Count > 0)
                        {
                            // Fire-and-forget deletion to speed up returning to dashboard.
                            _ = _imageService.DeleteCacheFilesAsync(imageIds);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to delete cached images after forced stop: {ex.Message}");
                    }

                    DeactivateSecurityMode();
                    var dashboard = new DashboardWindow(_authService);
                    dashboard.Show();

                    // Supaya tidak ngetrigger ExamWindow_Closing logic yg nanya "submit" lagi
                    // Bikin flag temporer atau stop timer sblum dipanggil. 
                    // ExamWindow_Closing ngecek kalo kiosk ga aktif, trus stop aja dan bolehin keluar
                    this.Close();
                }
            }
        }

        private async void WifiButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Window
            {
                Title = "Wi-Fi Internal",
                Width = 520,
                Height = 460,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                WindowStyle = WindowStyle.ToolWindow,
                ResizeMode = ResizeMode.NoResize,
                Background = new SolidColorBrush(Color.FromRgb(248, 250, 252))
            };

            var root = new StackPanel { Margin = new Thickness(20) };

            root.Children.Add(new TextBlock
            {
                Text = "Pilih jaringan Wi-Fi cadangan yang tersedia",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(30, 58, 138)),
                Margin = new Thickness(0, 0, 0, 4)
            });

            root.Children.Add(new TextBlock
            {
                Text = "Lakukan pemindaian ulang jika daftar jaringan belum muncul atau masih menampilkan jaringan lama.",
                TextWrapping = TextWrapping.Wrap,
                Foreground = new SolidColorBrush(Color.FromRgb(75, 85, 99)),
                Margin = new Thickness(0, 0, 0, 10)
            });

            var connectedWifiText = new TextBlock
            {
                Text = "Wi-Fi terhubung saat ini: mendeteksi...",
                TextWrapping = TextWrapping.Wrap,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(30, 58, 138)),
                Margin = new Thickness(0, 0, 0, 14)
            };
            root.Children.Add(connectedWifiText);

            var scanButton = new Button
            {
                Content = "🔄 Pindai Ulang Jaringan",
                Style = (Style)FindResource("PolinemaButtonSecondary"),
                Padding = new Thickness(16, 10, 16, 10),
                Margin = new Thickness(0, 0, 0, 14)
            };
            root.Children.Add(scanButton);

            var networkBox = new ComboBox
            {
                Height = 40,
                Margin = new Thickness(0, 0, 0, 10),
                DisplayMemberPath = nameof(WifiNetworkOption.DisplayText)
            };
            root.Children.Add(networkBox);

            root.Children.Add(new TextBlock
            {
                Text = "Password Wi-Fi",
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(31, 41, 55)),
                Margin = new Thickness(0, 6, 0, 6)
            });

            var passwordHint = new TextBlock
            {
                Text = "Isi hanya jika jaringan memakai password. Jika Wi-Fi terbuka, biarkan kosong.",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128)),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 6)
            };
            root.Children.Add(passwordHint);

            var passwordBox = new PasswordBox
            {
                Height = 38,
                Margin = new Thickness(0, 0, 0, 16),
                Padding = new Thickness(10, 8, 10, 8)
            };
            root.Children.Add(passwordBox);

            var statusText = new TextBlock
            {
                Text = "Status: siap memindai.",
                Foreground = new SolidColorBrush(Color.FromRgb(75, 85, 99)),
                Margin = new Thickness(0, 0, 0, 12)
            };
            root.Children.Add(statusText);

            var connectButton = new Button
            {
                Content = "Hubungkan ke Wi-Fi",
                Style = (Style)FindResource("PolinemaButtonPrimary"),
                Padding = new Thickness(16, 10, 16, 10),
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            root.Children.Add(connectButton);

            var networksTemp = new List<WifiNetworkOption>();

            async Task RefreshConnectedWifiInfoAsync()
            {
                var connectedSsid = await GetConnectedWifiSsidAsync();
                if (string.IsNullOrWhiteSpace(connectedSsid))
                {
                    connectedWifiText.Text = "Wi-Fi terhubung saat ini: tidak ada";
                    connectedWifiText.Foreground = new SolidColorBrush(Color.FromRgb(180, 83, 9));
                }
                else
                {
                    connectedWifiText.Text = $"Wi-Fi terhubung saat ini: {connectedSsid}";
                    connectedWifiText.Foreground = new SolidColorBrush(Color.FromRgb(22, 101, 52));
                }
            }

            async Task RefreshNetworksAsync()
            {
                try
                {
                    await RefreshConnectedWifiInfoAsync();
                    statusText.Text = "Status: memindai jaringan...";
                    scanButton.IsEnabled = false;
                    connectButton.IsEnabled = false;
                    networkBox.ItemsSource = null;
                    networksTemp.Clear();

                    await NativeWifi.ScanNetworksAsync(timeout: TimeSpan.FromSeconds(4));

                    var scanned = new List<WifiNetworkOption>();
                    foreach (var wifiInterface in NativeWifi.EnumerateInterfaces())
                    {
                        try
                        {
                            var (scanResult, availableNetworks) = NativeWifi.EnumerateAvailableNetworks(wifiInterface.Id);
                            if (scanResult != ActionResult.Success)
                            {
                                continue;
                            }

                            foreach (var network in availableNetworks)
                            {
                                var ssid = network.Ssid.ToString();
                                if (string.IsNullOrWhiteSpace(ssid))
                                {
                                    continue;
                                }

                                scanned.Add(new WifiNetworkOption
                                {
                                    InterfaceId = wifiInterface.Id,
                                    Ssid = ssid,
                                    SignalQuality = (int)network.SignalQuality,
                                    BssType = network.BssType
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Wi-Fi scan per interface failed: {ex.Message}");
                        }
                    }

                    networksTemp.AddRange(scanned
                        .GroupBy(x => x.Ssid, StringComparer.OrdinalIgnoreCase)
                        .Select(g => g.OrderByDescending(x => x.SignalQuality).First())
                        .OrderByDescending(x => x.SignalQuality)
                        .ToList());

                    networkBox.ItemsSource = networksTemp;
                    if (networkBox.Items.Count > 0)
                    {
                        networkBox.SelectedIndex = 0;
                        statusText.Text = $"Status: {networksTemp.Count} jaringan ditemukan.";
                    }
                    else
                    {
                        statusText.Text = "Status: tidak ada jaringan Wi-Fi yang terdeteksi.";
                    }
                }
                catch (Exception ex)
                {
                    statusText.Text = $"Status: gagal memindai ({ex.Message})";
                }
                finally
                {
                    scanButton.IsEnabled = true;
                    connectButton.IsEnabled = true;
                }
            }

            scanButton.Click += async (_, __) => await RefreshNetworksAsync();

            connectButton.Click += async (_, __) =>
            {
                if (networkBox.SelectedItem is not WifiNetworkOption selectedNetwork)
                {
                    MessageBox.Show("Pilih jaringan Wi-Fi terlebih dahulu.", "Wi-Fi", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                try
                {
                    connectButton.IsEnabled = false;
                    scanButton.IsEnabled = false;
                    statusText.Text = $"Status: menyambungkan ke {selectedNetwork.Ssid}...";

                    string profileXml = string.IsNullOrWhiteSpace(passwordBox.Password)
                        ? $@"<?xml version=""1.0""?>
                        <WLANProfile xmlns=""http://www.microsoft.com/networking/WLAN/profile/v1"">
                            <name>{selectedNetwork.Ssid}</name>
                            <SSIDConfig><SSID><name>{selectedNetwork.Ssid}</name></SSID></SSIDConfig>
                            <connectionType>ESS</connectionType>
                            <connectionMode>manual</connectionMode>
                            <MSM><security><authEncryption><authentication>open</authentication><encryption>none</encryption><useOneX>false</useOneX></authEncryption></security></MSM>
                        </WLANProfile>"
                                                : $@"<?xml version=""1.0""?>
                        <WLANProfile xmlns=""http://www.microsoft.com/networking/WLAN/profile/v1"">
                            <name>{selectedNetwork.Ssid}</name>
                            <SSIDConfig><SSID><name>{selectedNetwork.Ssid}</name></SSID></SSIDConfig>
                            <connectionType>ESS</connectionType>
                            <connectionMode>manual</connectionMode>
                            <MSM>
                                <security>
                                    <authEncryption><authentication>WPA2PSK</authentication><encryption>AES</encryption><useOneX>false</useOneX></authEncryption>
                                    <sharedKey><keyType>passPhrase</keyType><protected>false</protected><keyMaterial>{passwordBox.Password}</keyMaterial></sharedKey>
                                </security>
                            </MSM>
                        </WLANProfile>";

                    NativeWifi.SetProfile(selectedNetwork.InterfaceId, ProfileType.AllUser, profileXml, null, true);
                    var isConnected = await NativeWifi.ConnectNetworkAsync(selectedNetwork.InterfaceId, selectedNetwork.Ssid, selectedNetwork.BssType, TimeSpan.FromSeconds(20));

                    if (isConnected)
                    {
                        await RefreshConnectedWifiInfoAsync();
                        MessageBox.Show($"Berhasil terhubung ke {selectedNetwork.Ssid}.", "Wi-Fi", MessageBoxButton.OK, MessageBoxImage.Information);
                        dialog.Close();
                    }
                    else
                    {
                        statusText.Text = "Status: koneksi gagal, coba ulangi atau periksa password.";
                        MessageBox.Show("Gagal terhubung. Silakan cek password atau coba pindai ulang jaringan.", "Wi-Fi", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                catch (Exception ex)
                {
                    statusText.Text = $"Status: gagal menyambung ({ex.Message})";
                    MessageBox.Show($"Terjadi kesalahan: {ex.Message}", "Wi-Fi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    connectButton.IsEnabled = true;
                    scanButton.IsEnabled = true;
                }
            };

            dialog.Content = root;
            await RefreshNetworksAsync();
            dialog.ShowDialog();
        }

        private async Task<string?> GetConnectedWifiSsidAsync()
        {
            try
            {
                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "netsh",
                        Arguments = "wlan show interfaces",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                string output = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (string.IsNullOrWhiteSpace(output))
                {
                    return null;
                }

                var lines = output.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    if (line.Contains("BSSID", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var match = Regex.Match(line, "^\\s*SSID\\s*:\\s*(.+)$", RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        var ssid = match.Groups[1].Value.Trim();
                        if (!string.IsNullOrWhiteSpace(ssid))
                        {
                            return ssid;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to get connected Wi-Fi SSID: {ex.Message}");
            }

            return null;
        }

        private sealed class WifiNetworkOption
        {
            public Guid InterfaceId { get; init; }
            public string Ssid { get; init; } = string.Empty;
            public int SignalQuality { get; init; }
            public BssType BssType { get; init; }
            public string DisplayText => $"{Ssid} ({SignalQuality}%)";
            public override string ToString() => DisplayText;
        }

        /// <summary>
        /// Handles a State-Reconciliation breach: stops the exam, warns the student,
        /// releases the device binding in the database, and returns to the login screen.
        /// </summary>
        private async void OnSecurityBreachDetected()
        {
            _timer.Stop();

            // Silently record the forced termination in t_ujian_mahasiswa
            // (status='dihentikan', endtime, nilai, keterangan) before notifying the student
            await _examService.TerminateForBreachAsync(_ujianId, _mahasiswaId);
            await _examService.ClearDoubtStatesAsync(_ujianId, _mahasiswaId);

            MessageBox.Show(
                "⚠️ PELANGGARAN KEAMANAN TERDETEKSI!\n\n" +
                "Akun Anda terdeteksi aktif di perangkat lain.\n" +
                "Semua jawaban yang telah terisi telah disimpan dan dinilai.\n\n" +
                "Anda akan dikeluarkan dari ujian secara otomatis.",
                "Sesi Tidak Valid",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            DeactivateSecurityMode();
            await _authService.LogoutAsync();

            var loginWindow = new LoginWindow();
            loginWindow.Show();
            this.Close();
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

        /// <summary>
        /// Updates offline status UI visibility and styling with animation
        /// </summary>
        private void UpdateOfflineUIStatus(bool isOnline)
        {
            if (isOnline)
            {
                // Fade out offline badge animation
                if (OfflineStatusBorder.Visibility == Visibility.Visible)
                {
                    var fadeOut = (Storyboard)this.FindResource("BadgeFadeOutStoryboard");
                    fadeOut.Completed += (s, e) =>
                    {
                        OfflineStatusBorder.Visibility = Visibility.Collapsed;
                    };
                    fadeOut.Begin(OfflineStatusBorder);
                }

                // Also fade out and hide pending count badge
                if (PendingCountBorder.Visibility == Visibility.Visible)
                {
                    var fadeOut = (Storyboard)this.FindResource("BadgeFadeOutStoryboard");
                    fadeOut.Completed += (s, e) =>
                    {
                        PendingCountBorder.Visibility = Visibility.Collapsed;
                    };
                    fadeOut.Begin(PendingCountBorder);
                }

                OfflineStatusText.Text = "Terhubung";
            }
            else
            {
                // Show and fade in offline badge
                OfflineStatusBorder.Visibility = Visibility.Visible;
                OfflineStatusBorder.Opacity = 0;
                OfflineStatusText.Text = "Jawaban disimpan otomatis";

                var fadeIn = (Storyboard)this.FindResource("BadgeFadeInStoryboard");
                fadeIn.Begin(OfflineStatusBorder);
            }
        }

        /// <summary>
        /// Updates pending status message based on pending count with animation pulse
        /// </summary>
        private void UpdatePendingStatusText(int pendingCount)
        {
            // Trigger pulse animation on pending badge
            if (pendingCount > 0 && PendingCountBorder.Visibility == Visibility.Visible)
            {
                try
                {
                    var pulse = (Storyboard)this.FindResource("PulseScaleStoryboard");
                    pulse.Begin(PendingCountText.Parent as FrameworkElement);
                }
                catch { }
            }

            if (pendingCount == 0)
            {
                PendingStatusText.Text = "Sinkronisasi & kirim";
            }
            else if (pendingCount == 1)
            {
                PendingStatusText.Text = "1 jawaban menunggu";
            }
            else
            {
                PendingStatusText.Text = $"{pendingCount} jawaban menunggu";
            }
        }

        /// <summary>
        /// Shows a progress dialog while syncing pending answers to server
        /// </summary>
        private async Task ShowSyncProgressAsync(int initialPendingCount)
        {
            var syncWindow = new Window
            {
                Title = "Sinkronisasi Jawaban",
                Width = 500,
                Height = 280,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                WindowStyle = WindowStyle.ToolWindow,
                ResizeMode = ResizeMode.NoResize,
                Background = new SolidColorBrush(Color.FromRgb(248, 250, 252))
            };

            var root = new StackPanel { Margin = new Thickness(30) };

            root.Children.Add(new TextBlock
            {
                Text = "Sinkronisasi Jawaban",
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(30, 58, 138)),
                Margin = new Thickness(0, 0, 0, 10)
            });

            var statusText = new TextBlock
            {
                Text = $"Mengirim {initialPendingCount} jawaban...",
                FontSize = 14,
                Foreground = new SolidColorBrush(Color.FromRgb(75, 85, 99)),
                Margin = new Thickness(0, 0, 0, 15)
            };
            root.Children.Add(statusText);

            var progressBar = new ProgressBar
            {
                Height = 8,
                Background = new SolidColorBrush(Color.FromRgb(229, 231, 235)),
                Foreground = new SolidColorBrush(Color.FromRgb(34, 197, 94)),
                IsIndeterminate = true,
                Margin = new Thickness(0, 0, 0, 15)
            };
            root.Children.Add(progressBar);

            var infoText = new TextBlock
            {
                Text = "Jangan tutup aplikasi ini sambil sinkronisasi sedang berlangsung.\nIni mungkin membutuhkan beberapa detik tergantung kecepatan koneksi Anda.",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128)),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 15)
            };
            root.Children.Add(infoText);

            syncWindow.Content = root;
            syncWindow.Show();

            // Monitor pending count until sync completes
            int lastPendingCount = initialPendingCount;
            var timeoutTimer = DateTime.Now.AddSeconds(60); // Max 60 second timeout

            while (lastPendingCount > 0 && DateTime.Now < timeoutTimer)
            {
                await Task.Delay(1000);
                lastPendingCount = await _examService.GetPendingCountAsync(_ujianId, _mahasiswaId);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    statusText.Text = lastPendingCount > 0 
                        ? $"Mengirim {lastPendingCount} jawaban..." 
                        : "Semua jawaban telah sinkronisasi!";
                });

                if (lastPendingCount == 0)
                {
                    progressBar.IsIndeterminate = false;
                    progressBar.Value = 100;
                }
            }

            // Close after brief delay to show completion
            await Task.Delay(500);

            if (syncWindow.IsLoaded)
            {
                syncWindow.Close();
            }
        }
    }
}
