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
                    case "OpenFolder":
                        if (!string.IsNullOrWhiteSpace(action.TargetPath))
                        {
                            var startInfo = new ProcessStartInfo
                            {
                                FileName = action.TargetPath,
                                UseShellExecute = true,
                                Arguments = action.Arguments ?? string.Empty
                            };
                            var process = Process.Start(startInfo);
                            
                            // If it's a program and we got a process handle, wait for it to create a window
                            if (process != null && action.ActionType == "RunProgram")
                            {
                                System.Threading.Tasks.Task.Run(() =>
                                {
                                    try
                                    {
                                        // Wait up to 5 seconds for the process to be ready for user input
                                        process.WaitForInputIdle(5000);
                                        System.Threading.Thread.Sleep(200); // Brief delay for window initialization
                                        
                                        process.Refresh(); // Refresh process properties to get MainWindowHandle
                                        if (process.MainWindowHandle != IntPtr.Zero)
                                        {
                                            SetForegroundWindow(process.MainWindowHandle);
                                            ShowWindow(process.MainWindowHandle, SW_RESTORE);
                                        }
                                    }
                                    catch { /* Process might not have a GUI, or exited, ignore */ }
                                });
                            }
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

        private const uint WM_CLOSE = 0x0010;
        private const int SW_MAXIMIZE = 3;
        private const int SW_MINIMIZE = 6;
        private const int SW_RESTORE = 9;
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