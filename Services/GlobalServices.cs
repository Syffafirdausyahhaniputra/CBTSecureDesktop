using System;
using CBTSecureDesktop.Security;

namespace CBTSecureDesktop.Services
{
    /// <summary>
    /// Global services singleton container untuk accessible di seluruh aplikasi.
    /// Mengelola lifecycle dari services seperti PerformanceMonitoringService.
    /// </summary>
    public static class GlobalServices
    {
        /// <summary>
        /// Global instance PerformanceMonitoringService yang berjalan sepanjang app lifecycle.
        /// Hanya untuk tracking metrics, TIDAK untuk killing aplikasi.
        /// </summary>
        public static PerformanceMonitoringService? PerformanceMonitoring { get; private set; }

        /// <summary>
        /// Startup time aplikasi untuk tracking total uptime.
        /// </summary>
        public static DateTime ApplicationStartTime { get; private set; }

        /// <summary>
        /// Flag untuk indicate apakah aplikasi sedang shutting down.
        /// </summary>
        public static bool IsShuttingDown { get; set; }

        /// <summary>
        /// Initialize global services saat aplikasi startup.
        /// </summary>
        public static void Initialize()
        {
            try
            {
                ApplicationStartTime = DateTime.Now;
                PerformanceMonitoring = new PerformanceMonitoringService();
                System.Diagnostics.Debug.WriteLine($"GlobalServices initialized at {ApplicationStartTime:yyyy-MM-dd HH:mm:ss}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing GlobalServices: {ex.Message}");
            }
        }

        /// <summary>
        /// Start performance monitoring background task.
        /// </summary>
        public static void StartPerformanceMonitoring()
        {
            try
            {
                PerformanceMonitoring?.StartMonitoring();
                System.Diagnostics.Debug.WriteLine("GlobalServices: Performance monitoring started");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error starting performance monitoring: {ex.Message}");
            }
        }

        /// <summary>
        /// Get total application uptime dalam seconds.
        /// </summary>
        public static long GetApplicationUptimeSeconds()
        {
            return (long)(DateTime.Now - ApplicationStartTime).TotalSeconds;
        }

        /// <summary>
        /// Get formatted uptime string.
        /// </summary>
        public static string GetApplicationUptimeFormatted()
        {
            var uptime = TimeSpan.FromSeconds(GetApplicationUptimeSeconds());
            return $"{uptime.Hours:D2}:{uptime.Minutes:D2}:{uptime.Seconds:D2}";
        }

        /// <summary>
        /// Shutdown global services secara graceful.
        /// </summary>
        public static async Task ShutdownAsync()
        {
            try
            {
                IsShuttingDown = true;

                if (PerformanceMonitoring != null)
                {
                    // Stop monitoring
                    PerformanceMonitoring.StopMonitoring();
                    System.Diagnostics.Debug.WriteLine("GlobalServices: Performance monitoring stopped");

                    // Generate performance report
                    await PerformanceMonitoring.GenerateReportAsync();
                    System.Diagnostics.Debug.WriteLine("GlobalServices: Performance report generated");

                    // Cleanup
                    PerformanceMonitoring.Dispose();
                }

                System.Diagnostics.Debug.WriteLine("GlobalServices shutdown complete");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during GlobalServices shutdown: {ex.Message}");
            }
        }

        /// <summary>
        /// Get performance summary statistics.
        /// </summary>
        public static Dictionary<string, object> GetPerformanceSummary()
        {
            if (PerformanceMonitoring == null)
                return new Dictionary<string, object>();

            var summary = PerformanceMonitoring.GetSummaryStatistics();

            // Add uptime info
            summary["total_uptime_seconds"] = GetApplicationUptimeSeconds();
            summary["total_uptime_formatted"] = GetApplicationUptimeFormatted();
            summary["session_duration"] = (DateTime.Now - ApplicationStartTime).ToString(@"hh\:mm\:ss");

            return summary;
        }

        /// <summary>
        /// Get paths ke performance log files.
        /// </summary>
        public static (string TextLog, string CSVLog, string Report) GetMetricsLogPaths()
        {
            if (PerformanceMonitoring == null)
                return (string.Empty, string.Empty, string.Empty);

            return PerformanceMonitoring.GetLogPaths();
        }
    }
}
