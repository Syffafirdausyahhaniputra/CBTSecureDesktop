using System;
using System.Collections.Generic; // [MODIFIKASI] Ditambahkan untuk List<>
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media; // [MODIFIKASI] Ditambahkan untuk pewarnaan SolidColorBrush
using System.Windows.Threading; // [MODIFIKASI]: Dibutuhkan untuk Timer pemantau layar
using Microsoft.Win32;

namespace CBTSecureDesktop.Security
{
    /// <summary>
    /// Manages Kiosk Mode to create a secure exam environment.
    /// Fullscreen window with hidden taskbar and disabled system navigation.
    /// </summary>
    public class KioskManager
    {
        private Window? _kioskWindow;
        private WindowState _previousWindowState;
        private WindowStyle _previousWindowStyle;
        private bool _previousTopmost;
        private ResizeMode _previousResizeMode;

        // [MODIFIKASI] List untuk menyimpan referensi jendela hitam di layar tambahan
        private List<Window> _blackoutWindows = new List<Window>();

        // [MODIFIKASI]: Timer untuk terus-menerus mengecek apakah mahasiswa mencolok kabel HDMI di tengah ujian
        private DispatcherTimer? _monitorCheckTimer;

        // [MODIFIKASI]: Import Windows API untuk mengecek jumlah koneksi fisik monitor dari Kartu Grafis
        [DllImport("user32.dll")]
        private static extern int GetDisplayConfigBufferSizes(uint flags, out uint numPathArrayElements, out uint numModeInfoArrayElements);
        private const uint QDC_ONLY_ACTIVE_PATHS = 2; // Hanya menghitung jalur tampilan yang aktif menyala

        // Windows API imports for taskbar manipulation
        [DllImport("user32.dll")]
        private static extern int FindWindow(string className, string windowText);

        [DllImport("user32.dll")]
        private static extern int ShowWindow(int hwnd, int command);

        private const int SW_HIDE = 0;
        private const int SW_SHOW = 1;

        /// <summary>
        /// Enables Kiosk Mode for the specified window.
        /// Makes window fullscreen, topmost, and hides the taskbar.
        /// </summary>
        /// <param name="window">The window to put into kiosk mode</param>
        public void EnableKioskMode(Window window)
        {
            if (window == null)
                throw new ArgumentNullException(nameof(window));

            _kioskWindow = window;

            // Store current window properties for restoration later
            _previousWindowState = window.WindowState;
            _previousWindowStyle = window.WindowStyle;
            _previousTopmost = window.Topmost;
            _previousResizeMode = window.ResizeMode;

            // Configure window for kiosk mode
            window.WindowStyle = WindowStyle.None;  // Remove title bar and borders
            window.ResizeMode = ResizeMode.NoResize; // Prevent resizing
            window.Topmost = true;                   // Always on top
            window.WindowState = WindowState.Maximized; // Fullscreen

            // Prevent window from being deactivated
            window.Deactivated += OnWindowDeactivated;

            // =================================================================
            // [MODIFIKASI]: Nonaktifkan Gesture Touchpad via Registry
            // =================================================================
            DisableTouchpadGestures();

            // =================================================================
            // [MODIFIKASI] Membunuh Windows Shell untuk memblokir Touchpad Gesture
            // =================================================================
            KillWindowsShell();

            // Hide Windows taskbar for complete lockdown
            HideTaskbar();

            // =================================================================
            // [MODIFIKASI] Blokir Layar Ganda (Dual Monitor / Multi Display)
            // =================================================================
            BlockSecondaryMonitors();

            // =================================================================
            // [MODIFIKASI]: Mulai pemantauan mode Duplicate secara Real-Time
            // =================================================================
            StartPhysicalMonitorCheck();
        }

        /// <summary>
        /// Disables Kiosk Mode and restores the window to its previous state.
        /// </summary>
        public void DisableKioskMode()
        {
            if (_kioskWindow == null)
                return;

            // =================================================================
            // [MODIFIKASI]: Matikan pemantauan saat ujian selesai
            // =================================================================
            StopPhysicalMonitorCheck();

            // Restore original window properties
            _kioskWindow.WindowStyle = _previousWindowStyle;
            _kioskWindow.ResizeMode = _previousResizeMode;
            _kioskWindow.Topmost = _previousTopmost;
            _kioskWindow.WindowState = _previousWindowState;

            // Remove event handler
            _kioskWindow.Deactivated -= OnWindowDeactivated;

            // Show taskbar again
            ShowTaskbar();

            // =================================================================
            // [MODIFIKASI]: Mengaktifkan kembali Gesture Touchpad via Registry
            // =================================================================
            RestoreTouchpadGestures();

            // =================================================================
            // [MODIFIKASI] Menghidupkan kembali Windows Shell setelah ujian selesai
            // =================================================================
            RestoreWindowsShell();

            // =================================================================
            // [MODIFIKASI] Hapus Jendela Hitam dari Layar Ganda
            // =================================================================
            RestoreSecondaryMonitors();

            _kioskWindow = null;
        }

        // =================================================================
        // [MODIFIKASI] BLOK FUNGSI BARU UNTUK REGISTRY TOUCHPAD
        // =================================================================
        private void DisableTouchpadGestures()
        {
            try
            {
                // Mengakses pengaturan Precision Touchpad di Registry Windows
                using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\PrecisionTouchPad", true))
                {
                    if (key != null)
                    {
                        // Set nilai 0 untuk mematikan slide 3 dan 4 jari
                        key.SetValue("ThreeFingerSlideEnabled", 0, RegistryValueKind.DWord);
                        key.SetValue("FourFingerSlideEnabled", 0, RegistryValueKind.DWord);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Gagal mematikan gesture touchpad di Registry: {ex.Message}");
            }
        }

        private void RestoreTouchpadGestures()
        {
            try
            {
                using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\PrecisionTouchPad", true))
                {
                    if (key != null)
                    {
                        // Set nilai 1 untuk menghidupkan kembali slide 3 dan 4 jari
                        key.SetValue("ThreeFingerSlideEnabled", 1, RegistryValueKind.DWord);
                        key.SetValue("FourFingerSlideEnabled", 1, RegistryValueKind.DWord);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Gagal mengembalikan gesture touchpad di Registry: {ex.Message}");
            }
        }
        // =================================================================

        // =================================================================
        // [MODIFIKASI] BLOK FUNGSI BARU UNTUK DETEKSI MODE DUPLICATE/CLONE
        // =================================================================
        private void StartPhysicalMonitorCheck()
        {
            // Mengecek kondisi layar setiap 2 detik
            _monitorCheckTimer = new DispatcherTimer();
            _monitorCheckTimer.Interval = TimeSpan.FromSeconds(2);
            _monitorCheckTimer.Tick += (s, e) =>
            {
                CheckForDuplicateMonitors();
            };
            _monitorCheckTimer.Start();
        }

        private void StopPhysicalMonitorCheck()
        {
            if (_monitorCheckTimer != null)
            {
                _monitorCheckTimer.Stop();
                _monitorCheckTimer = null;
            }
        }

        private void CheckForDuplicateMonitors()
        {
            try
            {
                // Mengambil jumlah layar logika (Yang dilihat oleh Windows)
                int logicalScreens = System.Windows.Forms.Screen.AllScreens.Length;

                // Mengambil jumlah jalur fisik (Kabel HDMI/DisplayPort/Miracast yang aktif)
                int result = GetDisplayConfigBufferSizes(QDC_ONLY_ACTIVE_PATHS, out uint physicalPaths, out uint modeCount);

                if (result == 0) // Jika pembacaan API sukses
                {
                    // LOGIKA INTI: Jika layar logika cuma 1, tapi kabel fisik yang tersambung > 1, itu pasti Duplicate Mode!
                    if (logicalScreens == 1 && physicalPaths > 1)
                    {
                        // Hentikan timer agar pesan error tidak muncul berkali-kali
                        _monitorCheckTimer?.Stop();

                        // =================================================================
                        // [MODIFIKASI]: Mengikat MessageBox langsung ke layar ujian 
                        // tanpa harus menonaktifkan status Topmost
                        // =================================================================
                        if (_kioskWindow != null)
                        {
                            // Menambahkan _kioskWindow sebagai "Owner" (Parameter 1),
                            // sehingga MessageBox otomatis mewarisi sifat Topmost layar ujian.
                            MessageBox.Show(
                                _kioskWindow, 
                                "Keamanan Sistem: Layar Ganda (Duplicate/Mirroring) terdeteksi!\n\n" +
                                "Menampilkan soal ujian ke layar lain tidak diperbolehkan. " +
                                "Silakan cabut kabel monitor eksternal Anda atau putuskan koneksi cast layar.",
                                "Pelanggaran CBT",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
                        }
                        else
                        {
                            MessageBox.Show(
                                "Keamanan Sistem: Layar Ganda (Duplicate/Mirroring) terdeteksi!\n\n" +
                                "Menampilkan soal ujian ke layar lain tidak diperbolehkan. " +
                                "Silakan cabut kabel monitor eksternal Anda atau putuskan koneksi cast layar.",
                                "Pelanggaran CBT",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
                        }
                        // =================================================================

                        // Tutup paksa aplikasi (Shutdown) setelah mahasiswa menekan tombol OK
                        Application.Current.Shutdown();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Gagal mendeteksi perangkat keras monitor: {ex.Message}");
            }
        }
        // =================================================================

        // =================================================================
        // [MODIFIKASI] FUNGSI BARU UNTUK WINDOWS SHELL (EXPLORER.EXE)
        // =================================================================
        private void KillWindowsShell()
        {
            try
            {
                // Menghentikan explorer.exe mematikan taskbar, Start Menu, dan Gesture Touchpad
                Process[] explorers = Process.GetProcessesByName("explorer");
                foreach (Process explorer in explorers)
                {
                    explorer.Kill();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Gagal mematikan Windows Shell: {ex.Message}");
            }
        }

        private void RestoreWindowsShell()
        {
            try
            {
                // Cek jika explorer belum berjalan, maka hidupkan kembali
                if (Process.GetProcessesByName("explorer").Length == 0)
                {
                    Process.Start("explorer.exe");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Gagal menghidupkan Windows Shell: {ex.Message}");
            }
        }
        // =================================================================

        // =================================================================
        // [MODIFIKASI] FUNGSI BARU UNTUK MULTI-MONITOR
        // =================================================================
        private void BlockSecondaryMonitors()
        {
            try
            {
                // System.Windows.Forms diperlukan untuk mendeteksi semua layar yang aktif
                foreach (var screen in System.Windows.Forms.Screen.AllScreens)
                {
                    // Jika layar tersebut BUKAN layar utama, buat jendela hitam di atasnya
                    if (!screen.Primary)
                    {
                        var blackoutWin = new Window
                        {
                            Background = System.Windows.Media.Brushes.Black, // Layar hitam
                            WindowStyle = WindowStyle.None,
                            ResizeMode = ResizeMode.NoResize,
                            ShowInTaskbar = false,
                            Topmost = true, // Pastikan selalu di atas
                            WindowStartupLocation = WindowStartupLocation.Manual,
                            Left = screen.Bounds.Left,
                            Top = screen.Bounds.Top,
                            Width = screen.Bounds.Width,
                            Height = screen.Bounds.Height
                        };

                        blackoutWin.Show();
                        _blackoutWindows.Add(blackoutWin);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Gagal memblokir layar ganda: {ex.Message}");
            }
        }

        private void RestoreSecondaryMonitors()
        {
            foreach (var win in _blackoutWindows)
            {
                win.Close();
            }
            _blackoutWindows.Clear();
        }
        // =================================================================

        /// <summary>
        /// Prevents the kiosk window from losing focus.
        /// This ensures the student cannot interact with other applications.
        /// </summary>
        private void OnWindowDeactivated(object? sender, EventArgs e)
        {
            if (_kioskWindow != null)
            {
                // [MODIFIKASI TAMBAHAN]: Memaksa fokus dengan lebih agresif jika mahasiswa berhasil lolos ke desktop lain
                _kioskWindow.Topmost = false;
                _kioskWindow.Topmost = true;
                _kioskWindow.Activate();
                _kioskWindow.Focus();
            }
        }

        /// <summary>
        /// Hides the Windows taskbar using Win32 API.
        /// </summary>
        private void HideTaskbar()
        {
            try
            {
                int hwnd = FindWindow("Shell_TrayWnd", "");
                if (hwnd > 0)
                {
                    ShowWindow(hwnd, SW_HIDE);
                }
            }
            catch (Exception ex)
            {
                // Log error but continue - security feature degradation
                System.Diagnostics.Debug.WriteLine($"Failed to hide taskbar: {ex.Message}");
            }
        }

        /// <summary>
        /// Shows the Windows taskbar using Win32 API.
        /// </summary>
        private void ShowTaskbar()
        {
            try
            {
                int hwnd = FindWindow("Shell_TrayWnd", "");
                if (hwnd > 0)
                {
                    ShowWindow(hwnd, SW_SHOW);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to show taskbar: {ex.Message}");
            }
        }

        /// <summary>
        /// Checks if Kiosk Mode is currently active.
        /// </summary>
        public bool IsKioskModeActive => _kioskWindow != null;
    }
}