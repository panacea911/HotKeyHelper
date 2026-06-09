using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.IO;

namespace HotKeyHelper
{
    public class HotKeyContext : ApplicationContext
    {
        private NotifyIcon _trayIcon;
        private KeyboardHook _keyboardHook;
        private AppConfig _config;

        public HotKeyContext()
        {
            _config = ConfigManager.LoadConfig();

            // Initialize Tray Icon
            Icon? appIcon = null;
            try
            {
                using (var stream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("HotKeyHelper.app_icon.png"))
                {
                    if (stream != null)
                    {
                        using (var bitmap = new Bitmap(stream))
                        {
                            appIcon = Icon.FromHandle(bitmap.GetHicon());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load tray icon: {ex.Message}");
            }

            _trayIcon = new NotifyIcon()
            {
                Icon = appIcon ?? SystemIcons.Application,
                ContextMenuStrip = new ContextMenuStrip(),
                Visible = true,
                Text = "Менеджер горячих клавиш"
            };

            _trayIcon.ContextMenuStrip.Items.Add("Настройки", null, ShowSettings);
            _trayIcon.ContextMenuStrip.Items.Add(new ToolStripSeparator());
            _trayIcon.ContextMenuStrip.Items.Add("Выход", null, Exit);

            _trayIcon.DoubleClick += ShowSettings;

            // Initialize Hook
            _keyboardHook = new KeyboardHook();
            _keyboardHook.KeyPressed += OnKeyPressed;
        }

        private void OnKeyPressed(object? sender, KeyPressedEventArgs e)
        {
            if (Application.OpenForms.OfType<KeyCatchForm>().Any())
                return; // Let the Catch form handle the keys without executing actions

            foreach (var action in _config.Hotkeys)
            {
                if (IsMatch(action.KeyCombination, e.Key, e.Modifiers))
                {
                    ActionExecutor.Execute(action);
                    e.Handled = true; // Block system from processing
                    break;
                }
            }
        }

        private bool IsMatch(string combination, Keys key, ModifierKeys modifiers)
        {
            if (string.IsNullOrWhiteSpace(combination)) return false;

            var parts = combination.Split('+').Select(p => p.Trim().ToLower()).ToList();
            
            bool reqCtrl = parts.Contains("control") || parts.Contains("ctrl");
            bool reqAlt = parts.Contains("alt");
            bool reqShift = parts.Contains("shift");
            bool reqWin = parts.Contains("win") || parts.Contains("windows");

            bool hasCtrl = (modifiers & ModifierKeys.Control) == ModifierKeys.Control;
            bool hasAlt = (modifiers & ModifierKeys.Alt) == ModifierKeys.Alt;
            bool hasShift = (modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;
            bool hasWin = (modifiers & ModifierKeys.Windows) == ModifierKeys.Windows;

            if (reqCtrl != hasCtrl || reqAlt != hasAlt || reqShift != hasShift || reqWin != hasWin)
                return false;

            string mainKeyStr = parts.LastOrDefault() ?? "";
            if (Enum.TryParse(mainKeyStr, true, out Keys parsedKey))
            {
                return key == parsedKey;
            }

            return false;
        }

        private void ShowSettings(object? sender, EventArgs e)
        {
            // If already open, bring to front
            var form = Application.OpenForms.OfType<SettingsForm>().FirstOrDefault();
            if (form == null)
            {
                form = new SettingsForm(_config, _keyboardHook);
                form.ConfigSaved += (s, newConfig) =>
                {
                    _config = newConfig;
                    ConfigManager.SaveConfig(_config);
                    ManageAutostart(_config.RunOnStartup);
                };
                form.Show();
            }
            else
            {
                form.Activate();
            }
        }

        private void ManageAutostart(bool enable)
        {
            string startupFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            string shortcutPathLnk = Path.Combine(startupFolderPath, "HotKeyHelper.lnk");
            string shortcutPathUrl = Path.Combine(startupFolderPath, "HotKeyHelper.url");

            if (enable)
            {
                if (File.Exists(shortcutPathUrl)) File.Delete(shortcutPathUrl);

                if (!File.Exists(shortcutPathLnk))
                {
                    string exePath = Environment.ProcessPath ?? Application.ExecutablePath;
                    try
                    {
                        IShellLink link = (IShellLink)new ShellLink();
                        link.SetPath(exePath);
                        link.SetWorkingDirectory(Path.GetDirectoryName(exePath) ?? "");
                        System.Runtime.InteropServices.ComTypes.IPersistFile file = (System.Runtime.InteropServices.ComTypes.IPersistFile)link;
                        file.Save(shortcutPathLnk, false);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to create shortcut: {ex.Message}");
                    }
                }
            }
            else
            {
                if (File.Exists(shortcutPathLnk)) File.Delete(shortcutPathLnk);
                if (File.Exists(shortcutPathUrl)) File.Delete(shortcutPathUrl);
            }
        }

        [System.Runtime.InteropServices.ComImport]
        [System.Runtime.InteropServices.Guid("00021401-0000-0000-C000-000000000046")]
        internal class ShellLink
        {
        }

        [System.Runtime.InteropServices.ComImport]
        [System.Runtime.InteropServices.InterfaceType(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown)]
        [System.Runtime.InteropServices.Guid("000214F9-0000-0000-C000-000000000046")]
        internal interface IShellLink
        {
            void GetPath([System.Runtime.InteropServices.Out, System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] System.Text.StringBuilder pszFile, int cchMaxPath, out IntPtr pfd, int fFlags);
            void GetIDList(out IntPtr ppidl);
            void SetIDList(IntPtr pidl);
            void GetDescription([System.Runtime.InteropServices.Out, System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] System.Text.StringBuilder pszName, int cchMaxName);
            void SetDescription([System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] string pszName);
            void GetWorkingDirectory([System.Runtime.InteropServices.Out, System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] System.Text.StringBuilder pszDir, int cchMaxPath);
            void SetWorkingDirectory([System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] string pszDir);
            void GetArguments([System.Runtime.InteropServices.Out, System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] System.Text.StringBuilder pszArgs, int cchMaxPath);
            void SetArguments([System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] string pszArgs);
            void GetHotkey(out short pwHotkey);
            void SetHotkey(short wHotkey);
            void GetShowCmd(out int piShowCmd);
            void SetShowCmd(int iShowCmd);
            void GetIconLocation([System.Runtime.InteropServices.Out, System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] System.Text.StringBuilder pszIconPath, int cchIconPath, out int piIcon);
            void SetIconLocation([System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
            void SetRelativePath([System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] string pszPathRel, int dwReserved);
            void Resolve(IntPtr hwnd, int fFlags);
            void SetPath([System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] string pszFile);
        }

        private void Exit(object? sender, EventArgs e)
        {
            _trayIcon.Visible = false;
            _keyboardHook.Dispose();
            Application.Exit();
        }
    }
}