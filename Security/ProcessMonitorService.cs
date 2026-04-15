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

        public ProcessMonitorService()
        {
            // Default forbidden processes (without .exe extension as ProcessName usually excludes it)
            _forbiddenProcesses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "chrome",
                "msedge",
                "firefox",
                "obs64",
                "discord",
                "taskmgr"
                // "cmd",         // Dinonaktifkan sementara untuk pengujian lokal
                // "powershell"   // Dinonaktifkan sementara untuk pengujian lokal
            };

            // Setup log file path in the application directory
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var appFolder = Path.Combine(appDataPath, "CBTSecureDesktop");
            if (!Directory.Exists(appFolder))
            {
                Directory.CreateDirectory(appFolder);
            }
            _logFilePath = Path.Combine(appFolder, "logs.txt");
        }

        /// <summary>
        /// Starts background monitoring of running processes.
        /// </summary>
        /// <param name="onViolationDetected">Optional callback when a violation is detected.</param>
        public void StartMonitoring(Action<string>? onViolationDetected = null)
        {
            if (_isRunning) return;

            _isRunning = true;
            _cancellationTokenSource = new CancellationTokenSource();

            // Run on a background thread
            Task.Run(() => MonitorLoopAsync(_cancellationTokenSource.Token, onViolationDetected), _cancellationTokenSource.Token);

            LogEvent("System Process Monitoring started.");
        }

        /// <summary>
        /// Stops the background monitoring safely.
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

        private async Task MonitorLoopAsync(CancellationToken token, Action<string>? onViolationDetected)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    // Get all currently running processes
                    var currentProcesses = Process.GetProcesses();

                    foreach (var process in currentProcesses)
                    {
                        try
                        {
                            if (_forbiddenProcesses.Contains(process.ProcessName))
                            {
                                string processName = process.ProcessName;

                                // Option 1: Try to kill the process
                                if (!process.HasExited)
                                {
                                    process.Kill();

                                    // Option 3: Log the violation
                                    LogViolation(processName);

                                    // Option 2: Notify the UI/App via callback
                                    onViolationDetected?.Invoke(processName);
                                }
                            }
                        }
                        catch (System.ComponentModel.Win32Exception)
                        {
                            // Occurs if the process is elevated (admin) and our app is not elevated.
                            // We might not be able to interact with or kill it.
                        }
                        catch (InvalidOperationException)
                        {
                            // The process has already exited.
                        }
                        finally
                        {
                            // Release resources associated with the process component
                            process.Dispose();
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogEvent($"Error during process monitoring scan: {ex.Message}");
                }

                try
                {
                    // Wait 1-2 seconds before next scan to keep CPU usage low
                    await Task.Delay(1500, token);
                }
                catch (TaskCanceledException)
                {
                    // Ignore expected exception on cancellation
                }
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
