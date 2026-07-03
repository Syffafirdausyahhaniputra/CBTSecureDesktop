using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace CBTSecureDesktop.Security
{
    public class ProcessMonitorService
    {
        private bool _isRunning;
        private CancellationTokenSource? _cancellationTokenSource;
        private readonly HashSet<string> _forbiddenProcesses;
        private readonly HashSet<string> _allowedProcesses;
        private readonly string _logFilePath;

        public ProcessMonitorService()
        {
            // Daftar hitam proses aplikasi terlarang demi menjaga integritas Secure CBT
            // StringComparer.OrdinalIgnoreCase memastikan deteksi kebal dari variasi huruf besar/kecil
            _forbiddenProcesses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                // 1. Web Browsers
                "chrome", "msedge", "firefox", "opera", "brave", "vivaldi", "safari",

                // 2. Remote Desktop & Screen Sharing
                "TeamViewer", "AnyDesk", "RustDesk", "UltraViewer_Service", "UltraViewer", "mstsc",

                // 3. Screen Recording & Streaming
                "obs64", "obs32", "ShareX", "Streamlabs OBS", "bdcam", "fraps", "action",

                // 4. Communication, Social Media & Chat Apps
                "discord", "Telegram", "WhatsApp", "slack", "Teams", "zoom", "instagram", "line", "skype",

                // 5. Virtualization Software
                "VirtualBox", "vmware", "vboxservice", "vmdkloop", "vpxclient",

                // 6. Windows System Utilities
                "taskmgr", "cmd", "powershell", "mmc", "regedit",

                // 7. Microsoft Office Productivity Tools
                "winword", "excel", "powerpnt", "onenote", "outlook",

                // 8. Text Editors, IDEs & AI-Powered Development Tools
                "notepad", "notepad++", "sublime_text",
                "code",         // Visual Studio Code (Sering dipasang ekstensi AI)
                "devenv",       // Microsoft Visual Studio (Community/Professional/Enterprise)
                "cursor",       // Cursor (IDE spesifik AI yang sangat populer saat ini)
                "zed",          // Zed IDE (Memiliki fitur AI terintegrasi)
                "windsurf",     // Windsurf (IDE AI dari Codeium)
                "idea64",       // IntelliJ IDEA (JetBrains - ada AI Assistant)
                "idea",         // IntelliJ IDEA (Versi 32-bit jika ada)
                "pycharm64",    // PyCharm (JetBrains)
                "pycharm",      // PyCharm 32-bit
                "studio64",     // Android Studio (Memiliki integrasi Gemini)
                "antigravity",  // Sesuai permintaan spesifik
                "ChatGPT",      // Desktop App ChatGPT resmi dari OpenAI
                "Claude",       // Desktop App Claude resmi dari Anthropic

                // 9. Aplikasi Microsoft Store (Universal Windows Platform)
                // Catatan Keterbatasan: Aplikasi UWP dibungkus oleh ApplicationFrameHost.
                // Jika aplikasi UWP tidak tertutup, ini dikarenakan arsitektur Runtime Broker Windows.
                "ApplicationFrameHost", // Host universal untuk jendela aplikasi dari Microsoft Store
                "CalculatorApp",        // Kalkulator bawaan Windows
                "WinStore.App",         // Aplikasi Microsoft Store itu sendiri
                "SnippingTool",         // Snipping tool Windows
                "YourPhone"             // Aplikasi Phone Link Windows
            };

            // =================================================================
            // [MODIFIKASI]: Inisialisasi proses pengecualian yang aman
            // =================================================================
            _allowedProcesses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "webview2", // Mengecualikan msedgewebview2 yang digunakan OS / WPF
                "hub"       // (Opsional) Jika nanti Anda memblokir 'github' tapi butuh 'VpnHub' dll, bisa disesuaikan
            };

            // Inisialisasi folder penyimpanan berkas log keamanan di AppData lokal mahasiswa
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var appFolder = Path.Combine(appDataPath, "CBTSecureDesktop");
            if (!Directory.Exists(appFolder))
            {
                Directory.CreateDirectory(appFolder);
            }
            _logFilePath = Path.Combine(appFolder, "logs.txt");
        }

        /// <summary>
        /// Memulai pemantauan background thread untuk memeriksa proses yang sedang berjalan.
        /// </summary>
        /// <param name="onViolationDetected">Callback yang dimodifikasi untuk mengirimkan satu ringkasan string berisi daftar aplikasi yang ditutup dalam satu scan.</param>
        public void StartMonitoring(Action<string>? onViolationDetected = null)
        {
            if (_isRunning) return;

            _isRunning = true;
            _cancellationTokenSource = new CancellationTokenSource();

            // Menjalankan loop pemindaian di dalam background thread terpisah agar UI WPF tidak freeze
            Task.Run(() => MonitorLoopAsync(_cancellationTokenSource.Token, onViolationDetected), _cancellationTokenSource.Token);

            LogEvent("System Process Monitoring started.");
        }

        /// <summary>
        /// Menghentikan background monitoring secara aman saat sesi ujian berakhir resmi.
        /// </summary>
        public void StopMonitoring()
        {
            if (!_isRunning) return;

            _isRunning = false;
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;

            LogEvent("System Process Monitoring stopped.");
        }

        /// <summary>
        /// Loop asinkronus yang berjalan berkala untuk memindai seluruh proses aktif di OS Windows.
        /// </summary>
        private async Task MonitorLoopAsync(CancellationToken token, Action<string>? onViolationDetected)
        {
            while (!token.IsCancellationRequested)
            {
                // List lokal untuk menampung nama-nama aplikasi terlarang yang ditutup pada siklus pemindaian saat ini
                var detectedInThisCycle = new List<string>();

                try
                {
                    // Menarik seluruh daftar objek proses yang sedang aktif di Windows kernel
                    var currentProcesses = Process.GetProcesses();

                    foreach (var process in currentProcesses)
                    {
                        try
                        {
                            string processName = process.ProcessName;

                            // [MODIFIKASI LOGIKA PENCARIAN]:
                            // Mengecek apakah processName MENGANDUNG salah satu kata dari daftar _forbiddenProcesses
                            bool isForbidden = _forbiddenProcesses.Any(forbiddenWord =>
                                processName.Contains(forbiddenWord, StringComparison.OrdinalIgnoreCase));

                            // =================================================================
                            // [MODIFIKASI]: Cek apakah mengandung kata yang dikecualikan
                            // =================================================================
                            bool isAllowed = _allowedProcesses.Any(allowedWord =>
                                processName.Contains(allowedWord, StringComparison.OrdinalIgnoreCase));

                            if (isForbidden && !isAllowed)
                            {
                                if (!process.HasExited)
                                {
                                    try
                                    {
                                        // Langkah Pertama: Kill normal menggunakan standard .NET API
                                        process.Kill();
                                    }
                                    catch (System.ComponentModel.Win32Exception)
                                    {
                                        // Langkah Cadangan (Fallback): Jika gagal karena hak akses admin, 
                                        // eksekusi perintah TASKKILL OS secara paksa (/F) via CMD senyap
                                        ForceKillViaOS(processName);
                                    }

                                    // Mencatat detail pelanggaran keamanan ke file log lokal teks
                                    LogViolation(processName);

                                    // Masukkan nama proses ke dalam list siklus ini jika belum terdaftar
                                    if (!detectedInThisCycle.Contains(processName))
                                    {
                                        detectedInThisCycle.Add(processName);
                                    }
                                }
                            }
                        }
                        catch (System.ComponentModel.Win32Exception)
                        {
                            // Terjadi jika proses berjalan dalam mode elevated (admin) sedangkan aplikasi CBT tidak berjalan sebagai admin.
                        }
                        catch (InvalidOperationException)
                        {
                            // Mengantisipasi jika proses target sudah ditutup sendiri oleh sistem/user sebelum dieksekusi.
                        }
                        finally
                        {
                            // Melepaskan alokasi memori komponen handler proses untuk mencegah memory leak di laptop mahasiswa
                            process.Dispose();
                        }
                    }

                    // KETENTUAN MODIFIKASI 1: Jika ada aplikasi terlarang yang terdeteksi ditutup pada siklus ini,
                    // gabungkan daftarnya menjadi satu string pesan tunggal dan kirimkan ke UI.
                    if (detectedInThisCycle.Any() && onViolationDetected != null)
                    {
                        // Menghasilkan string terformat contoh: "Chrome, WinWord, Notepad"
                        string summaryMessage = string.Join(", ", detectedInThisCycle);
                        onViolationDetected.Invoke(summaryMessage);
                    }
                }
                catch (Exception ex)
                {
                    LogEvent($"Error during process monitoring scan: {ex.Message}");
                }

                // ================================================================
                // TAMBAHAN: Deteksi dan stop Windows Services yang terlarang
                // ================================================================
                try
                {
                    var stoppedServices = DetectAndStopForbiddenServices();

                    // Jika ada services yang di-stop, kirimkan notifikasi ke UI
                    if (stoppedServices.Any() && onViolationDetected != null)
                    {
                        string servicesSummary = $"[SERVICES STOPPED] {string.Join(", ", stoppedServices)}";
                        onViolationDetected.Invoke(servicesSummary);
                    }
                }
                catch (Exception ex)
                {
                    LogEvent($"Error during service monitoring scan: {ex.Message}");
                }

                try
                {
                    // Menunda pemindaian selama 1.5 detik sebelum scan berikutnya demi menjaga penggunaan CPU laptop tetap rendah
                    await Task.Delay(1500, token);
                }
                catch (TaskCanceledException)
                {
                    // Mengabaikan exception normal saat token pembatalan (StopMonitoring) dipicu
                }
            }
        }

        /// <summary>
        /// Mekanisme Fallback paksa menggunakan taskkill Windows untuk melumpuhkan aplikasi keras kepala (Service SYSTEM).
        /// </summary>
        private void ForceKillViaOS(string processName)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "taskkill",
                    Arguments = $"/F /IM {processName}.exe",
                    CreateNoWindow = true, // Berjalan senyap di latar belakang tanpa memunculkan kotak hitam CMD
                    UseShellExecute = false
                };

                using (var p = Process.Start(psi))
                {
                    p?.WaitForExit(1000); // Menunggu eksekusi OS maksimal 1 detik
                }
            }
            catch (Exception ex)
            {
                LogEvent($"Gagal mengeksekusi OS taskkill untuk {processName}: {ex.Message}");
            }
        }

        private async Task LogCurrentProcessPerformanceAsync(CancellationToken token)
        {
            // This method removed - performance logging is now handled by PerformanceMonitoringService
            // ProcessMonitorService focuses on security only - no performance tracking
            await Task.CompletedTask;
        }
        /// <summary>
        /// Mendeteksi dan menghentikan Windows Services yang terlarang berdasarkan daftar _forbiddenProcesses.
        /// Mengembalikan list nama services yang berhasil di-stop dalam siklus ini.
        /// </summary>
        private List<string> DetectAndStopForbiddenServices()
        {
            var stoppedServices = new List<string>();

            try
            {
                // Ambil semua services yang sedang berjalan di sistem
                ServiceController[] services = ServiceController.GetServices();

                foreach (var service in services)
                {
                    try
                    {
                        // Extract nama executable dari service path
                        string serviceExecutableName = ExtractServiceExecutableName(service.ServiceName);

                        if (string.IsNullOrEmpty(serviceExecutableName))
                            continue;

                        // Cek apakah nama executable termasuk dalam daftar terlarang
                        bool isForbidden = _forbiddenProcesses.Any(forbiddenWord =>
                            serviceExecutableName.Contains(forbiddenWord, StringComparison.OrdinalIgnoreCase));

                        // Cek apakah dikecualikan dari daftar putih
                        bool isAllowed = _allowedProcesses.Any(allowedWord =>
                            serviceExecutableName.Contains(allowedWord, StringComparison.OrdinalIgnoreCase));

                        if (isForbidden && !isAllowed && service.Status == ServiceControllerStatus.Running)
                        {
                            try
                            {
                                // Langkah Pertama: Stop service menggunakan standard .NET API
                                service.Stop();
                                service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(3));

                                LogViolation($"Service '{service.ServiceName}' (executable: {serviceExecutableName})");
                                stoppedServices.Add($"{service.ServiceName}");
                            }
                            catch (Exception stopEx)
                            {
                                Debug.WriteLine($"Failed to stop service {service.ServiceName}: {stopEx.Message}");

                                // Langkah Cadangan (Fallback): Gunakan sc stop command via OS
                                try
                                {
                                    ForceStopServiceViaOS(service.ServiceName);
                                    stoppedServices.Add($"{service.ServiceName} (forced)");
                                }
                                catch (Exception forceEx)
                                {
                                    Debug.WriteLine($"Failed to force stop service {service.ServiceName}: {forceEx.Message}");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Abaikan error untuk service individual, lanjutkan ke service berikutnya
                        Debug.WriteLine($"Error processing service {service.ServiceName}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                LogEvent($"Error during service monitoring scan: {ex.Message}");
            }

            return stoppedServices;
        }

        /// <summary>
        /// Mengekstrak nama executable dari path service yang tersimpan di registry Windows.
        /// Menangani berbagai format path seperti "C:\Program Files\App\app.exe" atau hanya nama service.
        /// </summary>
        private string ExtractServiceExecutableName(string serviceName)
        {
            try
            {
                // Coba ambil dari registry
                string registryPath = @"SYSTEM\CurrentControlSet\Services\" + serviceName;
                using (var regKey = Registry.LocalMachine.OpenSubKey(registryPath))
                {
                    if (regKey != null)
                    {
                        var imagePath = regKey.GetValue("ImagePath") as string;
                        if (!string.IsNullOrEmpty(imagePath))
                        {
                            // Extract nama file dari path penuh
                            // Contoh: "C:\Program Files\App\app.exe" → "app"
                            // Atau: "\"C:\Program Files\App\app.exe\" -arg" → "app"

                            // Hilangkan quote jika ada
                            imagePath = imagePath.Trim('"');

                            // Ambil bagian setelah "/" atau "\"
                            string fileName = Path.GetFileNameWithoutExtension(imagePath);
                            return fileName;
                        }
                    }
                }

                // Jika registry gagal, coba gunakan nama service sebagai fallback
                return serviceName;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error extracting executable name for service {serviceName}: {ex.Message}");
                return serviceName;
            }
        }

        /// <summary>
        /// Fallback mechanism untuk force stop service menggunakan sc command Windows.
        /// Ini digunakan jika standard API gagal atau permission denied.
        /// </summary>
        private void ForceStopServiceViaOS(string serviceName)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "net",
                    Arguments = $"stop \"{serviceName}\"",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using (var p = Process.Start(psi))
                {
                    p?.WaitForExit(5000); // Menunggu maksimal 5 detik
                }
            }
            catch (Exception ex)
            {
                LogEvent($"Failed to force stop service {serviceName} via OS: {ex.Message}");
            }
        }

        private void LogViolation(string processName)
        {
            string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] SECURITY VIOLATION: Blocked forbidden process '{processName}' during exam.";
            AppendToLogFile(logEntry);
            Debug.WriteLine(logEntry);
        }

        private void LogEvent(string message)
        {
            string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] INFO: {message}";
            AppendToLogFile(logEntry);
            Debug.WriteLine(logEntry);
        }

        private void AppendToLogFile(string message)
        {
            try
            {
                File.AppendAllText(_logFilePath, message + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to write to log file: {ex.Message}");
            }
        }
    }
}
