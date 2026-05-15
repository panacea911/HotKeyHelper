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
            _trayIcon = new NotifyIcon()
            {
                Icon = SystemIcons.Application,
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
            string shortcutPath = Path.Combine(startupFolderPath, "HotKeyHelper.url");

            if (enable)
            {
                if (!File.Exists(shortcutPath))
                {
                    string exePath = Application.ExecutablePath;
                    using (StreamWriter writer = new StreamWriter(shortcutPath))
                    {
                        writer.WriteLine("[InternetShortcut]");
                        writer.WriteLine("URL=file:///" + exePath.Replace('\\', '/'));
                        writer.WriteLine("IconIndex=0");
                        string icon = exePath.Replace('\\', '/');
                        writer.WriteLine("IconFile=" + icon);
                    }
                }
            }
            else
            {
                if (File.Exists(shortcutPath))
                {
                    File.Delete(shortcutPath);
                }
            }
        }

        private void Exit(object? sender, EventArgs e)
        {
            _trayIcon.Visible = false;
            _keyboardHook.Dispose();
            Application.Exit();
        }
    }
}