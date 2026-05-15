using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace HotKeyHelper
{
    public class KeyboardHook : IDisposable
    {
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_SYSKEYDOWN = 0x0104;

        private LowLevelKeyboardProc _proc;
        private IntPtr _hookID = IntPtr.Zero;

        public event EventHandler<KeyPressedEventArgs>? KeyPressed;

        public KeyboardHook()
        {
            _proc = HookCallback;
            _hookID = SetHook(_proc);
        }

        private IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule!)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc,
                    GetModuleHandle(curModule.ModuleName!), 0);
            }
        }

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN))
            {
                int vkCode = Marshal.ReadInt32(lParam);
                Keys key = (Keys)vkCode;

                var args = new KeyPressedEventArgs(key, GetModifiers());
                KeyPressed?.Invoke(this, args);

                if (args.Handled)
                {
                    // Block the key if handled
                    return (IntPtr)1;
                }
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        private ModifierKeys GetModifiers()
        {
            ModifierKeys modifiers = ModifierKeys.None;
            if ((GetKeyState((int)Keys.ControlKey) & 0x8000) != 0)
                modifiers |= ModifierKeys.Control;
            if ((GetKeyState((int)Keys.Menu) & 0x8000) != 0) // Alt
                modifiers |= ModifierKeys.Alt;
            if ((GetKeyState((int)Keys.ShiftKey) & 0x8000) != 0)
                modifiers |= ModifierKeys.Shift;
            if ((GetKeyState((int)Keys.LWin) & 0x8000) != 0 || (GetKeyState((int)Keys.RWin) & 0x8000) != 0)
                modifiers |= ModifierKeys.Windows;
            return modifiers;
        }

        public void Dispose()
        {
            UnhookWindowsHookEx(_hookID);
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook,
            LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
            IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true, CallingConvention = CallingConvention.Winapi)]
        private static extern short GetKeyState(int keyCode);
    }

    [Flags]
    public enum ModifierKeys
    {
        None = 0,
        Alt = 1,
        Control = 2,
        Shift = 4,
        Windows = 8
    }

    public class KeyPressedEventArgs : EventArgs
    {
        public Keys Key { get; }
        public ModifierKeys Modifiers { get; }
        public bool Handled { get; set; }

        public KeyPressedEventArgs(Keys key, ModifierKeys modifiers)
        {
            Key = key;
            Modifiers = modifiers;
        }
    }
}