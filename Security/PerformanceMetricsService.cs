using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using CBTSecureDesktop.Models;

namespace CBTSecureDesktop.Security
{
    /// <summary>
    /// Service untuk mengumpulkan dan mencatat metrik performa aplikasi CBT.
    /// Melacak CPU, memory, threads, handles, dan metrics lainnya untuk diagnostics.
    /// </summary>
    public class PerformanceMetricsService
    {
        private readonly string _metricsLogPath;
        private readonly string _metricsCSVPath;
        private readonly string _reportPath;
        private Process? _currentProcess;
        private DateTime _processStartTime;
        private List<PerformanceMetric> _collectedMetrics = new();
        private readonly object _metricsLock = new();

        // CPU tracking fields
        private double _lastCpuMs = 0;
        private DateTime _lastCpuTime = DateTime.UtcNow;

        public PerformanceMetricsService()
        {
            // Setup logging paths
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var appFolder = Path.Combine(appDataPath, "CBTSecureDesktop");
            if (!Directory.Exists(appFolder))
            {
                Directory.CreateDirectory(appFolder);
            }

            _metricsLogPath = Path.Combine(appFolder, "metrics.txt");
            _metricsCSVPath = Path.Combine(appFolder, "metrics.csv");
            _reportPath = Path.Combine(appFolder, "performance_report.txt");

            // Get current process reference
            _currentProcess = Process.GetCurrentProcess();
            _processStartTime = _currentProcess.StartTime;

            // Initialize CSV header jika file baru
            if (!File.Exists(_metricsCSVPath))
            {
                try
                {
                    File.WriteAllText(_metricsCSVPath, PerformanceMetric.GetCSVHeader() + Environment.NewLine);
                }
                catch { }
            }
        }

        /// <summary>
        /// Collect snapshot metrik performa saat ini.
        /// </summary>
        public PerformanceMetric CollectMetrics()
        {
            try
            {
                if (_currentProcess == null)
                    _currentProcess = Process.GetCurrentProcess();

                _currentProcess.Refresh();
                var networkIo = GetNetworkIoBytes();

                var metric = new PerformanceMetric
                {
                    Timestamp = PerformanceMetric.GetLocalTimestampNow(),
                    ProcessId = _currentProcess.Id,
                    CpuPercentage = GetCpuPercentage(),
                    MemoryMb = _currentProcess.WorkingSet64 / (1024d * 1024d),
                    VirtualMemoryMb = _currentProcess.VirtualMemorySize64 / (1024d * 1024d),
                    PrivateMemoryMb = _currentProcess.PrivateMemorySize64 / (1024d * 1024d),
                    ThreadCount = _currentProcess.Threads.Count,
                    HandleCount = _currentProcess.HandleCount,
                    RunningProcessCount = Process.GetProcesses().Length,
                    PageFaults = _currentProcess.NonpagedSystemMemorySize64,
                    UptimeSeconds = (long)(DateTime.Now - _processStartTime).TotalSeconds,
                    GarbageCollectionStats = GetGarbageCollectionStats(),
                    TotalSystemMemoryMb = GC.GetTotalMemory(false) / (1024d * 1024d),
                    AvailableSystemMemoryMb = GetAvailableMemory() / (1024d * 1024d),
                    NetworkBytesReceived = networkIo.Received,
                    NetworkBytesSent = networkIo.Sent,
                    NetworkIOMb = (networkIo.Received + networkIo.Sent) / (1024d * 1024d)
                };

                lock (_metricsLock)
                {
                    _collectedMetrics.Add(metric);
                }

                return metric;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error collecting metrics: {ex.Message}");
                return new PerformanceMetric();
            }
        }

        /// <summary>
        /// Hitung CPU percentage untuk aplikasi ini.
        /// </summary>
        private double GetCpuPercentage()
        {
            try
            {
                if (_currentProcess == null) return 0;

                var totalCpuTime = _currentProcess.TotalProcessorTime;
                var currentTime = DateTime.UtcNow;

                var diffCpuMs = (totalCpuTime.TotalMilliseconds - _lastCpuMs);
                var diffTime = (currentTime - _lastCpuTime).TotalMilliseconds;

                double cpuPercent = (diffCpuMs / (diffTime * Environment.ProcessorCount)) * 100.0;
                cpuPercent = Math.Max(0, cpuPercent);

                _lastCpuMs = totalCpuTime.TotalMilliseconds;
                _lastCpuTime = currentTime;

                return cpuPercent;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Get available system memory dalam bytes.
        /// </summary>
        private long GetAvailableMemory()
        {
            try
            {
                // Menggunakan GC info untuk estimate available memory
                var totalMemory = GC.GetTotalMemory(false);
                var totalPhysical = GC.GetTotalMemory(false) * 2; // Rough estimate
                return Math.Max(totalPhysical - totalMemory, 0);
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Get garbage collection statistics.
        /// </summary>
        private string GetGarbageCollectionStats()
        {
            try
            {
                // In .NET 8, use GCCollectionMode statistics alternative
                var totalMemory = GC.GetTotalMemory(false);
                return $"Memory:{totalMemory / (1024 * 1024)}MB";
            }
            catch
            {
                return "Memory:0MB";
            }
        }

        /// <summary>
        /// Gets host network bytes (sent/received) from active interfaces.
        /// </summary>
        private (long Received, long Sent) GetNetworkIoBytes()
        {
            try
            {
                long totalReceived = 0;
                long totalSent = 0;

                foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (networkInterface.OperationalStatus != OperationalStatus.Up)
                    {
                        continue;
                    }

                    if (networkInterface.NetworkInterfaceType == NetworkInterfaceType.Loopback ||
                        networkInterface.NetworkInterfaceType == NetworkInterfaceType.Tunnel)
                    {
                        continue;
                    }

                    var stats = networkInterface.GetIPv4Statistics();
                    totalReceived += stats.BytesReceived;
                    totalSent += stats.BytesSent;
                }

                return (totalReceived, totalSent);
            }
            catch
            {
                return (0, 0);
            }
        }

        /// <summary>
        /// Log metric ke file teks.
        /// </summary>
        public async Task LogMetricAsync(PerformanceMetric metric)
        {
            try
            {
                var logEntry = metric.ToDetailedString() + Environment.NewLine;

                await using var stream = new FileStream(
                    _metricsLogPath,
                    FileMode.Append,
                    FileAccess.Write,
                    FileShare.ReadWrite,
                    4096,
                    useAsync: true);

                await using var writer = new StreamWriter(stream);
                await writer.WriteLineAsync(logEntry);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error logging metric: {ex.Message}");
            }
        }

        /// <summary>
        /// Log metric ke file CSV untuk analytics.
        /// </summary>
        public async Task LogMetricAsCSVAsync(PerformanceMetric metric)
        {
            try
            {
                var csvEntry = metric.ToCSV();

                await using var stream = new FileStream(
                    _metricsCSVPath,
                    FileMode.Append,
                    FileAccess.Write,
                    FileShare.ReadWrite,
                    4096,
                    useAsync: true);

                await using var writer = new StreamWriter(stream);
                await writer.WriteLineAsync(csvEntry);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error logging metric to CSV: {ex.Message}");
            }
        }

        /// <summary>
        /// Generate performance report dari collected metrics.
        /// </summary>
        public async Task GenerateReportAsync()
        {
            try
            {
                string reportContent;

                // Build report content outside lock
                lock (_metricsLock)
                {
                    if (_collectedMetrics.Count == 0)
                        return;

                    var report = new System.Text.StringBuilder();
                    report.AppendLine("=== PERFORMANCE REPORT ===");
                    report.AppendLine($"Generated: {PerformanceMetric.GetLocalTimestampNow():yyyy-MM-dd HH:mm:ss zzz}");
                    report.AppendLine($"Total samples collected: {_collectedMetrics.Count}");
                    report.AppendLine();

                    // Statistics
                    report.AppendLine("--- STATISTICS ---");
                    var avgCpu = _collectedMetrics.Average(m => m.CpuPercentage);
                    var maxCpu = _collectedMetrics.Max(m => m.CpuPercentage);
                    var avgMemory = _collectedMetrics.Average(m => m.MemoryMb);
                    var maxMemory = _collectedMetrics.Max(m => m.MemoryMb);
                    var avgThreads = _collectedMetrics.Average(m => m.ThreadCount);
                    var maxThreads = _collectedMetrics.Max(m => m.ThreadCount);
                    var avgHandles = _collectedMetrics.Average(m => m.HandleCount);
                    var maxHandles = _collectedMetrics.Max(m => m.HandleCount);

                    report.AppendLine($"CPU - Avg: {avgCpu:F2}%, Max: {maxCpu:F2}%");
                    report.AppendLine($"Memory - Avg: {avgMemory:F1}MB, Max: {maxMemory:F1}MB");
                    report.AppendLine($"Threads - Avg: {avgThreads:F0}, Max: {maxThreads}");
                    report.AppendLine($"Handles - Avg: {avgHandles:F0}, Max: {maxHandles}");
                    report.AppendLine();

                    // Issues detected
                    report.AppendLine("--- ISSUES DETECTED ---");
                    var issues = _collectedMetrics.Where(m => m.IsPerformanceIssueDetected()).ToList();
                    if (issues.Any())
                    {
                        foreach (var issue in issues)
                        {
                            var diagnosis = issue.DiagnosePerformanceIssue();
                            if (diagnosis != null)
                            {
                                report.AppendLine($"[{issue.Timestamp:HH:mm:ss}] {diagnosis}");
                            }
                        }
                    }
                    else
                    {
                        report.AppendLine("No performance issues detected.");
                    }
                    report.AppendLine();

                    // Recommendations
                    report.AppendLine("--- RECOMMENDATIONS ---");
                    if (maxCpu > 80)
                        report.AppendLine("• High CPU usage detected. Optimize heavy computations or reduce background operations.");
                    if (maxMemory > 500)
                        report.AppendLine("• High memory usage detected. Check for memory leaks or optimize data structures.");
                    if (maxThreads > 100)
                        report.AppendLine("• High thread count detected. Ensure thread cleanup and reuse thread pools.");
                    if (maxHandles > 3000)
                        report.AppendLine("• High handle count detected. Ensure file handles and resources are properly closed.");

                    reportContent = report.ToString();
                }

                // Write to file outside lock
                await using var stream = new FileStream(
                    _reportPath,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.ReadWrite,
                    4096,
                    useAsync: true);

                await using var writer = new StreamWriter(stream);
                await writer.WriteAsync(reportContent);

                Debug.WriteLine($"Performance report generated: {_reportPath}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error generating report: {ex.Message}");
            }
        }

        /// <summary>
        /// Rotate/archive metrics files jika sudah terlalu besar.
        /// </summary>
        public void RotateMetricsFiles(long maxSizeMb = 50)
        {
            try
            {
                var maxSizeBytes = maxSizeMb * 1024 * 1024;

                // Check text log
                if (File.Exists(_metricsLogPath))
                {
                    var fileInfo = new FileInfo(_metricsLogPath);
                    if (fileInfo.Length > maxSizeBytes)
                    {
                        var backupPath = _metricsLogPath + $".{PerformanceMetric.GetLocalTimestampNow():yyyyMMdd_HHmmss}.bak";
                        File.Move(_metricsLogPath, backupPath, true);
                        Debug.WriteLine($"Metrics log rotated to: {backupPath}");
                    }
                }

                // Check CSV log
                if (File.Exists(_metricsCSVPath))
                {
                    var fileInfo = new FileInfo(_metricsCSVPath);
                    if (fileInfo.Length > maxSizeBytes)
                    {
                        var backupPath = _metricsCSVPath + $".{PerformanceMetric.GetLocalTimestampNow():yyyyMMdd_HHmmss}.bak";
                        File.Move(_metricsCSVPath, backupPath, true);

                        // Re-create CSV dengan header
                        File.WriteAllText(_metricsCSVPath, PerformanceMetric.GetCSVHeader() + Environment.NewLine);
                        Debug.WriteLine($"Metrics CSV rotated to: {backupPath}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error rotating metrics files: {ex.Message}");
            }
        }

        /// <summary>
        /// Get statistik ringkas dari metrics yang sudah dikumpulkan.
        /// </summary>
        public Dictionary<string, object> GetSummaryStatistics()
        {
            lock (_metricsLock)
            {
                if (_collectedMetrics.Count == 0)
                    return new Dictionary<string, object>();

                return new Dictionary<string, object>
                {
                    { "sample_count", _collectedMetrics.Count },
                    { "cpu_avg", _collectedMetrics.Average(m => m.CpuPercentage) },
                    { "cpu_max", _collectedMetrics.Max(m => m.CpuPercentage) },
                    { "memory_avg_mb", _collectedMetrics.Average(m => m.MemoryMb) },
                    { "memory_max_mb", _collectedMetrics.Max(m => m.MemoryMb) },
                    { "threads_avg", _collectedMetrics.Average(m => m.ThreadCount) },
                    { "threads_max", _collectedMetrics.Max(m => m.ThreadCount) },
                    { "handles_avg", _collectedMetrics.Average(m => m.HandleCount) },
                    { "handles_max", _collectedMetrics.Max(m => m.HandleCount) },
                    { "issues_detected", _collectedMetrics.Count(m => m.IsPerformanceIssueDetected()) }
                };
            }
        }

        /// <summary>
        /// Clear in-memory metrics untuk free memory.
        /// </summary>
        public void ClearMetrics()
        {
            lock (_metricsLock)
            {
                _collectedMetrics.Clear();
            }
        }

        /// <summary>
        /// Get log file paths.
        /// </summary>
        public (string TextLog, string CSVLog, string Report) GetLogPaths()
        {
            return (_metricsLogPath, _metricsCSVPath, _reportPath);
        }
    }
}
