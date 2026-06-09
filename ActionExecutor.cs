using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace HotKeyHelper
{
    public static class ActionExecutor
    {
        public static void Execute(HotkeyAction action)
        {
            try
            {
                switch (action.ActionType)
                {
                    case "RunProgram":
                        if (!string.IsNullOrWhiteSpace(action.TargetPath))
                        {
                            var startInfo = new ProcessStartInfo
                            {
                                FileName = action.TargetPath,
                                UseShellExecute = true,
                                Arguments = action.Arguments ?? string.Empty
                            };
                            var process = Process.Start(startInfo);
                            
                            if (process != null)
                            {
                                System.Threading.Tasks.Task.Run(() =>
                                {
                                    try
                                    {
                                        // Wait up to 5 seconds for the process to be ready for user input
                                        process.WaitForInputIdle(5000);
                                        System.Threading.Thread.Sleep(200); // Brief delay for window initialization
                                        
                                        process.Refresh(); // Refresh process properties to get MainWindowHandle
                                        IntPtr targetWnd = process.MainWindowHandle;
                                        if (targetWnd == IntPtr.Zero)
                                        {
                                            targetWnd = FindWindowForProcess(process.Id);
                                        }

                                        if (targetWnd != IntPtr.Zero)
                                        {
                                            ForceForegroundWindow(targetWnd);
                                        }
                                    }
                                    catch { /* Process might not have a GUI, or exited, ignore */ }
                                });
                            }
                        }
                        break;
                    case "OpenFolder":
                        if (!string.IsNullOrWhiteSpace(action.TargetPath))
                        {
                            var startInfo = new ProcessStartInfo
                            {
                                FileName = action.TargetPath,
                                UseShellExecute = true,
                                Arguments = action.Arguments ?? string.Empty
                            };
                            Process.Start(startInfo);
                        }
                        break;
                    case "CloseWindow":
                    {
                        IntPtr handle = GetForegroundWindow();
                        if (handle != IntPtr.Zero)
                        {
                            PostMessage(handle, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
                        }
                        break;
                    }
                    case "KillProcess":
                    {
                        IntPtr handle = GetForegroundWindow();
                        if (handle != IntPtr.Zero)
                        {
                            GetWindowThreadProcessId(handle, out uint processId);
                            if (processId > 0)
                            {
                                Process.GetProcessById((int)processId).Kill();
                            }
                        }
                        break;
                    }
                    case "ToggleMaximize":
                    {
                        IntPtr handle = GetForegroundWindow();
                        if (handle != IntPtr.Zero)
                        {
                            WINDOWPLACEMENT placement = new WINDOWPLACEMENT();
                            placement.length = Marshal.SizeOf(placement);
                            GetWindowPlacement(handle, ref placement);

                            if (placement.showCmd == SW_SHOWMAXIMIZED)
                            {
                                ShowWindow(handle, SW_RESTORE);
                            }
                            else
                            {
                                ShowWindow(handle, SW_MAXIMIZE);
                            }
                        }
                        break;
                    }
                    case "MinimizeWindow":
                    {
                        IntPtr handle = GetForegroundWindow();
                        if (handle != IntPtr.Zero)
                        {
                            ShowWindow(handle, SW_MINIMIZE);
                        }
                        break;
                    }
                    case "LockScreen":
                        LockWorkStation();
                        break;
                    case "VolumeUp":
                        keybd_event((byte)MediaKeys.VolumeUp, 0, KEYEVENTF_EXTENDEDKEY, IntPtr.Zero);
                        break;
                    case "VolumeDown":
                        keybd_event((byte)MediaKeys.VolumeDown, 0, KEYEVENTF_EXTENDEDKEY, IntPtr.Zero);
                        break;
                    case "VolumeMute":
                        keybd_event((byte)MediaKeys.VolumeMute, 0, KEYEVENTF_EXTENDEDKEY, IntPtr.Zero);
                        break;
                    case "MediaPlayPause":
                        keybd_event((byte)MediaKeys.MediaPlayPause, 0, KEYEVENTF_EXTENDEDKEY, IntPtr.Zero);
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to execute action {action.Name}: {ex.Message}");
            }
        }

        private static Process? FindRunningProcess(string targetPath)
        {
            try
            {
                string targetName = Path.GetFileNameWithoutExtension(targetPath);
                if (string.IsNullOrWhiteSpace(targetName)) return null;

                var processes = Process.GetProcessesByName(targetName);
                foreach (var p in processes)
                {
                    try
                    {
                        if (p.MainModule != null && string.Equals(p.MainModule.FileName, targetPath, StringComparison.OrdinalIgnoreCase))
                        {
                            return p;
                        }
                    }
                    catch
                    {
                    }
                }

                foreach (var p in processes)
                {
                    try
                    {
                        IntPtr hWnd = p.MainWindowHandle;
                        if (hWnd == IntPtr.Zero)
                        {
                            hWnd = FindWindowForProcess(p.Id);
                        }
                        if (hWnd != IntPtr.Zero)
                        {
                            return p;
                        }
                    }
                    catch { }
                }
            }
            catch { }
            return null;
        }

        public static bool ForceForegroundWindow(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero) return false;

            if (IsIconic(hWnd))
            {
                ShowWindow(hWnd, SW_RESTORE);
            }
            else
            {
                ShowWindow(hWnd, SW_SHOW);
            }

            if (SetForegroundWindow(hWnd))
            {
                return true;
            }

            IntPtr foregroundWnd = GetForegroundWindow();
            uint foregroundThreadId = GetWindowThreadProcessId(foregroundWnd, out uint dummy);
            uint currentThreadId = GetCurrentThreadId();

            if (foregroundThreadId != currentThreadId)
            {
                AttachThreadInput(currentThreadId, foregroundThreadId, true);
                BringWindowToTop(hWnd);
                SetForegroundWindow(hWnd);
                AttachThreadInput(currentThreadId, foregroundThreadId, false);
            }
            else
            {
                BringWindowToTop(hWnd);
                SetForegroundWindow(hWnd);
            }

            return GetForegroundWindow() == hWnd;
        }

        public static IntPtr FindWindowForProcess(int processId)
        {
            IntPtr foundWindow = IntPtr.Zero;
            EnumWindows((hWnd, lParam) =>
            {
                GetWindowThreadProcessId(hWnd, out uint windowProcessId);
                if (windowProcessId == processId && IsWindowVisible(hWnd))
                {
                    if (GetWindowTextLength(hWnd) > 0)
                    {
                        foundWindow = hWnd;
                        return false;
                    }
                }
                return true;
            }, IntPtr.Zero);
            return foundWindow;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool LockWorkStation();

        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, IntPtr dwExtraInfo);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();

        [DllImport("user32.dll")]
        private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

        [DllImport("user32.dll")]
        private static extern bool BringWindowToTop(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int GetWindowTextLength(IntPtr hWnd);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        private const uint WM_CLOSE = 0x0010;
        private const int SW_MAXIMIZE = 3;
        private const int SW_MINIMIZE = 6;
        private const int SW_RESTORE = 9;
        private const int SW_SHOW = 5;
        private const int SW_SHOWMAXIMIZED = 3;
        private const uint KEYEVENTF_EXTENDEDKEY = 0x0001;

        private struct WINDOWPLACEMENT
        {
            public int length;
            public int flags;
            public int showCmd;
            public System.Drawing.Point ptMinPosition;
            public System.Drawing.Point ptMaxPosition;
            public System.Drawing.Rectangle rcNormalPosition;
        }

        private enum MediaKeys : byte
        {
            VolumeMute = 0xAD,
            VolumeDown = 0xAE,
            VolumeUp = 0xAF,
            MediaNextTrack = 0xB0,
            MediaPreviousTrack = 0xB1,
            MediaStop = 0xB2,
            MediaPlayPause = 0xB3
        }
    }
}