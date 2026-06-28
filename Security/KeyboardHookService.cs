using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace CBTSecureDesktop.Security
{
    /// <summary>
    /// Implements a low-level keyboard hook to intercept and block dangerous key combinations
    /// that could allow students to exit the exam or access other applications.
    /// </summary>
    public class KeyboardHookService
    {
        // Windows API constants
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_SYSKEYDOWN = 0x0104;

        // Virtual key codes for blocked keys
        private const int VK_TAB = 0x09;
        private const int VK_ESCAPE = 0x1B;
        private const int VK_LWIN = 0x5B;
        private const int VK_RWIN = 0x5C;
        private const int VK_F4 = 0x73;

        // [MODIFIKASI] Konstanta tambahan untuk mendeteksi Panah Kiri/Kanan (Gesture Touchpad)
        private const int VK_LEFT = 0x25;
        private const int VK_RIGHT = 0x27;

        // Hook delegate and handle
        private LowLevelKeyboardProc? _proc;
        private IntPtr _hookID = IntPtr.Zero;

        // Delegate for the keyboard hook callback
        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        #region Windows API Imports
        // ... [KODE IMPORT DLL TETAP SAMA SEPERTI ASLINYA] ...
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);
        #endregion

        public void StartHook()
        {
            if (_hookID != IntPtr.Zero)
                return; // Hook already active

            _proc = HookCallback;
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule? curModule = curProcess.MainModule)
            {
                if (curModule != null)
                {
                    _hookID = SetWindowsHookEx(WH_KEYBOARD_LL, _proc,
                        GetModuleHandle(curModule.ModuleName), 0);
                }
            }

            if (_hookID == IntPtr.Zero)
            {
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            }

            Debug.WriteLine("Keyboard hook activated - Secure mode enabled");
        }

        public void StopHook()
        {
            if (_hookID != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookID);
                _hookID = IntPtr.Zero;
                Debug.WriteLine("Keyboard hook deactivated - Normal mode restored");
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN))
            {
                int vkCode = Marshal.ReadInt32(lParam);

                // =================================================================
                // [MODIFIKASI] Cegah Shortcut Touchpad (3/4 Jari) SEBELUM WinKey diblokir
                // =================================================================

                // Blokir WIN + CTRL + LEFT / RIGHT (Pindah Virtual Desktop)
                if ((vkCode == VK_LEFT || vkCode == VK_RIGHT) && IsCtrlPressed() && IsWinPressed())
                {
                    Debug.WriteLine("Blocked: WIN + CTRL + ARROW (Virtual Desktop Gesture)");
                    return (IntPtr)1;
                }

                // Blokir WIN + TAB (Task View / Multi-desktop view)
                if (vkCode == VK_TAB && IsWinPressed())
                {
                    Debug.WriteLine("Blocked: WIN + TAB (Task View Gesture)");
                    return (IntPtr)1;
                }
                // =================================================================

                // Block Windows Key (Left or Right)
                if (vkCode == VK_LWIN || vkCode == VK_RWIN)
                {
                    Debug.WriteLine("Blocked: Windows Key");
                    return (IntPtr)1; // Block the key
                }

                // Block ALT + TAB (task switcher)
                if (vkCode == VK_TAB && IsAltPressed())
                {
                    Debug.WriteLine("Blocked: ALT + TAB");
                    return (IntPtr)1; // Block the key
                }

                // Block ALT + F4 (close window)
                if (vkCode == VK_F4 && IsAltPressed())
                {
                    Debug.WriteLine("Blocked: ALT + F4");
                    return (IntPtr)1; // Block the key
                }

                // Block CTRL + ESC (Start menu)
                if (vkCode == VK_ESCAPE && IsCtrlPressed())
                {
                    Debug.WriteLine("Blocked: CTRL + ESC");
                    return (IntPtr)1; // Block the key
                }
            }

            // Pass the key to the next hook or to Windows
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        private bool IsAltPressed()
        {
            return (GetAsyncKeyState(0x12) & 0x8000) != 0; // VK_MENU = ALT
        }

        private bool IsCtrlPressed()
        {
            return (GetAsyncKeyState(0x11) & 0x8000) != 0; // VK_CONTROL = CTRL
        }

        // =================================================================
        // [MODIFIKASI] Fungsi helper baru untuk mengecek status tombol Windows
        // =================================================================
        private bool IsWinPressed()
        {
            return (GetAsyncKeyState(VK_LWIN) & 0x8000) != 0 || (GetAsyncKeyState(VK_RWIN) & 0x8000) != 0;
        }

        public bool IsHookActive => _hookID != IntPtr.Zero;

        ~KeyboardHookService()
        {
            StopHook();
        }
    }
}