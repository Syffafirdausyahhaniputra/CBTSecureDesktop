using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CBTSecureDesktop.Models;

namespace CBTSecureDesktop.Security
{
    /// <summary>
    /// Service terpisah untuk performance monitoring sepanjang application lifecycle.
    /// Berjalan dari startup hingga shutdown, independent dari security monitoring.
    /// TIDAK melakukan killing/stopping aplikasi - hanya tracking metrics.
    /// </summary>
    public class PerformanceMonitoringService : IDisposable
    {
        private bool _isMonitoring;
        private CancellationTokenSource? _cancellationTokenSource;
        private readonly PerformanceMetricsService _metricsService;
        private DateTime _startTimeUtc;
        private int _metricsCollectionCounter;

        public PerformanceMonitoringService()
        {
            _metricsService = new PerformanceMetricsService();
            _startTimeUtc = DateTime.UtcNow;
            _metricsCollectionCounter = 0;
        }

        /// <summary>
        /// Start performance monitoring background task.
        /// Tidak block UI, berjalan async di background.
        /// </summary>
        public void StartMonitoring()
        {
            if (_isMonitoring) return;

            _isMonitoring = true;
            _cancellationTokenSource = new CancellationTokenSource();

            Debug.WriteLine("PerformanceMonitoringService: Starting background monitoring");

            // Jalankan monitoring loop di background
            Task.Run(() => MonitoringLoopAsync(_cancellationTokenSource.Token), _cancellationTokenSource.Token);
        }

        /// <summary>
        /// Stop performance monitoring.
        /// </summary>
        public void StopMonitoring()
        {
            if (!_isMonitoring) return;

            _isMonitoring = false;
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;

            Debug.WriteLine("PerformanceMonitoringService: Monitoring stopped");
        }

        /// <summary>
        /// Background loop untuk collect dan log metrics.
        /// Berjalan setiap 1.5 detik.
        /// </summary>
        private async Task MonitoringLoopAsync(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        _metricsCollectionCounter++;

                        // Collect metrics
                        var metric = _metricsService.CollectMetrics();

                        // Log to text file (setiap iterasi)
                        await _metricsService.LogMetricAsync(metric);

                        // Log to CSV (setiap 2 iterasi untuk reduce I/O)
                        if (_metricsCollectionCounter % 2 == 0)
                        {
                            await _metricsService.LogMetricAsCSVAsync(metric);
                        }

                        // Rotate files if needed (setiap 20 iterasi)
                        if (_metricsCollectionCounter % 20 == 0)
                        {
                            _metricsService.RotateMetricsFiles(maxSizeMb: 50);
                        }

                        // Wait sebelum next collection
                        await Task.Delay(1500, token);
                    }
                    catch (TaskCanceledException)
                    {
                        // Normal when stopping
                        break;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error in monitoring loop: {ex.Message}");
                        // Terus jalan meski ada error
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fatal error in monitoring loop: {ex.Message}");
            }
        }

        /// <summary>
        /// Get total uptime dalam seconds.
        /// </summary>
        public long GetUptimeSeconds()
        {
            return (long)(DateTime.UtcNow - _startTimeUtc).TotalSeconds;
        }

        /// <summary>
        /// Get formatted uptime string HH:MM:SS.
        /// </summary>
        public string GetUptimeFormatted()
        {
            var uptime = TimeSpan.FromSeconds(GetUptimeSeconds());
            return $"{uptime.Hours:D2}:{uptime.Minutes:D2}:{uptime.Seconds:D2}";
        }

        /// <summary>
        /// Generate performance report asynchronously.
        /// </summary>
        public async Task GenerateReportAsync()
        {
            try
            {
                await _metricsService.GenerateReportAsync();
                Debug.WriteLine("PerformanceMonitoringService: Report generated");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error generating report: {ex.Message}");
            }
        }

        /// <summary>
        /// Get performance summary statistics.
        /// </summary>
        public Dictionary<string, object> GetSummaryStatistics()
        {
            var summary = _metricsService.GetSummaryStatistics();

            // Add uptime
            summary["total_uptime_seconds"] = GetUptimeSeconds();
            summary["total_uptime_formatted"] = GetUptimeFormatted();

            return summary;
        }

        /// <summary>
        /// Get paths ke log files.
        /// </summary>
        public (string TextLog, string CSVLog, string Report) GetLogPaths()
        {
            return _metricsService.GetLogPaths();
        }

        /// <summary>
        /// Clear in-memory metrics buffer.
        /// </summary>
        public void ClearMetricsBuffer()
        {
            _metricsService.ClearMetrics();
        }

        /// <summary>
        /// Cleanup resources.
        /// </summary>
        public void Dispose()
        {
            StopMonitoring();
            _cancellationTokenSource?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
