using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CBTSecureDesktop.Security
{
    public class ProcessMonitorService
    {
        private bool _isRunning;
        private CancellationTokenSource? _cancellationTokenSource;
        private readonly HashSet<string> _forbiddenProcesses;
        private readonly string _logFilePath;
        private readonly string _performanceLogFilePath;
        private TimeSpan _lastCpuTotalProcessorTime;
        private DateTime _lastCpuSampleTimeUtc;

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

            // Inisialisasi folder penyimpanan berkas log keamanan di AppData lokal mahasiswa
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var appFolder = Path.Combine(appDataPath, "CBTSecureDesktop");
            if (!Directory.Exists(appFolder))
            {
                Directory.CreateDirectory(appFolder);
            }
            _logFilePath = Path.Combine(appFolder, "logs.txt");
            _performanceLogFilePath = Path.Combine(appFolder, "log_performa_cbt.txt");
            _lastCpuSampleTimeUtc = DateTime.UtcNow;
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
                            // Memeriksa apakah nama proses saat ini terdaftar di dalam HashSet _forbiddenProcesses
                            if (_forbiddenProcesses.Contains(process.ProcessName))
                            {
                                string processName = process.ProcessName;

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

                await LogCurrentProcessPerformanceAsync(token);

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
            try
            {
                using var currentProcess = Process.GetCurrentProcess();

                double ramMb = currentProcess.WorkingSet64 / (1024d * 1024d);

                DateTime nowUtc = DateTime.UtcNow;
                TimeSpan currentCpu = currentProcess.TotalProcessorTime;

                double cpuPercent = 0d;
                double elapsedMs = (nowUtc - _lastCpuSampleTimeUtc).TotalMilliseconds;

                if (elapsedMs > 0)
                {
                    double cpuUsedMs = (currentCpu - _lastCpuTotalProcessorTime).TotalMilliseconds;
                    cpuPercent = (cpuUsedMs / (elapsedMs * Environment.ProcessorCount)) * 100d;
                    cpuPercent = Math.Max(0d, cpuPercent);
                }

                _lastCpuSampleTimeUtc = nowUtc;
                _lastCpuTotalProcessorTime = currentCpu;

                string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] RAM: {ramMb:F1} MB | CPU: {cpuPercent:F1}%";

                await using var stream = new FileStream(
                    _performanceLogFilePath,
                    FileMode.Append,
                    FileAccess.Write,
                    FileShare.ReadWrite,
                    4096,
                    useAsync: true);

                await using var writer = new StreamWriter(stream);
                await writer.WriteLineAsync(logEntry.AsMemory(), token);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to write performance log: {ex.Message}");
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