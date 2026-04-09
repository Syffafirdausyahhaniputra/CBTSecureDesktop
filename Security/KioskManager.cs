using System.Runtime.InteropServices;
using System.Windows;

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

            // Hide Windows taskbar for complete lockdown
            HideTaskbar();
        }

        /// <summary>
        /// Disables Kiosk Mode and restores the window to its previous state.
        /// </summary>
        public void DisableKioskMode()
        {
            if (_kioskWindow == null)
                return;

            // Restore original window properties
            _kioskWindow.WindowStyle = _previousWindowStyle;
            _kioskWindow.ResizeMode = _previousResizeMode;
            _kioskWindow.Topmost = _previousTopmost;
            _kioskWindow.WindowState = _previousWindowState;

            // Remove event handler
            _kioskWindow.Deactivated -= OnWindowDeactivated;

            // Show taskbar again
            ShowTaskbar();

            _kioskWindow = null;
        }

        /// <summary>
        /// Prevents the kiosk window from losing focus.
        /// This ensures the student cannot interact with other applications.
        /// </summary>
        private void OnWindowDeactivated(object? sender, EventArgs e)
        {
            if (_kioskWindow != null)
            {
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
