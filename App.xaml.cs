using System.Configuration;
using System.Data;
using System.Windows;
using CBTSecureDesktop.Services;
using System.Diagnostics;

namespace CBTSecureDesktop
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        /// <summary>
        /// Dijalankan saat aplikasi startup, sebelum main window ditampilkan.
        /// Initialize global services dan start performance monitoring (BUKAN security monitoring).
        /// Security monitoring hanya saat exam berlangsung.
        /// </summary>
        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                Debug.WriteLine("=== CBT SECURE DESKTOP APPLICATION STARTUP ===");
                Debug.WriteLine($"Startup Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                Debug.WriteLine($".NET Runtime: {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}");
                Debug.WriteLine($"OS: {System.Runtime.InteropServices.RuntimeInformation.OSDescription}");
                Debug.WriteLine($"Processor Count: {Environment.ProcessorCount}");
                Debug.WriteLine($"Total Memory: {GC.GetTotalMemory(false) / (1024 * 1024)}MB");

                // Initialize global services (PerformanceMonitoringService saja, bukan ProcessMonitor)
                GlobalServices.Initialize();

                // Start PERFORMANCE monitoring (track metrics, NO killing apps)
                GlobalServices.StartPerformanceMonitoring();
                Debug.WriteLine("Performance monitoring started - Will track metrics throughout session");

                Debug.WriteLine("Application startup complete - Ready for login");

                // Proceed dengan normal startup
                base.OnStartup(e);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"CRITICAL ERROR during startup: {ex.Message}");
                Debug.WriteLine($"StackTrace: {ex.StackTrace}");
                MessageBox.Show(
                    $"Error starting application: {ex.Message}",
                    "Startup Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                this.Shutdown(1);
            }
        }

        /// <summary>
        /// Dijalankan saat aplikasi akan ditutup.
        /// Stop monitoring, generate report, dan graceful shutdown.
        /// </summary>
        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                Debug.WriteLine("=== CBT SECURE DESKTOP APPLICATION EXIT ===");
                Debug.WriteLine($"Exit Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                Debug.WriteLine($"Session Duration: {GlobalServices.GetApplicationUptimeFormatted()}");

                // Execute shutdown synchronously (blocking operation)
                // Kita gunakan RunSynchronously untuk wait async method di context synchronous
                var shutdownTask = GlobalServices.ShutdownAsync();

                // Wait dengan timeout 5 detik untuk complete shutdown
                if (!shutdownTask.Wait(TimeSpan.FromSeconds(5)))
                {
                    Debug.WriteLine("WARNING: Shutdown did not complete within timeout");
                }

                // Performance data tetap dicatat ke file lokal oleh shutdown service
                Debug.WriteLine("Application exit complete");

                // Proceed dengan normal exit
                base.OnExit(e);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ERROR during exit: {ex.Message}");
                Debug.WriteLine($"StackTrace: {ex.StackTrace}");
                base.OnExit(e);
            }
        }
    }

}
