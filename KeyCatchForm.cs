using System;
using System.Windows.Forms;
using System.Drawing;

namespace HotKeyHelper
{
    public partial class KeyCatchForm : Form
    {
        public string ResultCombination { get; private set; } = "";

        private Label lblKeys;
        private KeyboardHook _hook;

        public KeyCatchForm(string current, KeyboardHook hook)
        {
            ResultCombination = current;
            _hook = hook;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Запись комбинации клавиш";
            this.Size = new Size(450, 200);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(30, 30, 30);
            this.ForeColor = Color.White;
            this.Font = new Font("Segoe UI", 10);

            var topLbl = new Label { Text = "Нажмите желаемую комбинацию клавиш...", Dock = DockStyle.Top, TextAlign = ContentAlignment.MiddleCenter, Height = 40, ForeColor = Color.LightGray };
            lblKeys = new Label { Text = string.IsNullOrEmpty(ResultCombination) ? "Ожидание..." : ResultCombination, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 16, FontStyle.Bold), ForeColor = Color.Cyan };
            
            var panel = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 50, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(5) };
            var btnSave = new Button { Text = "Применить", Width = 120, Height = 35, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(0, 122, 204), Cursor = Cursors.Hand };
            var btnCancel = new Button { Text = "Отмена", Width = 100, Height = 35, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(60, 60, 60), Margin = new Padding(10, 0, 0, 0), Cursor = Cursors.Hand };

            btnSave.FlatAppearance.BorderSize = 0;
            btnCancel.FlatAppearance.BorderSize = 0;

            btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };
            btnSave.Click += (s, e) => { this.DialogResult = DialogResult.OK; this.Close(); };

            panel.Controls.Add(btnCancel);
            panel.Controls.Add(btnSave);

            this.Controls.Add(lblKeys);
            this.Controls.Add(topLbl);
            this.Controls.Add(panel);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            _hook.KeyPressed += Hook_KeyPressed;
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _hook.KeyPressed -= Hook_KeyPressed;
            base.OnFormClosed(e);
        }

        private void Hook_KeyPressed(object? sender, KeyPressedEventArgs e)
        {
            // Ignore pure modifiers
            if (e.Key == Keys.LControlKey || e.Key == Keys.RControlKey || e.Key == Keys.LShiftKey || e.Key == Keys.RShiftKey || e.Key == Keys.LMenu || e.Key == Keys.RMenu || e.Key == Keys.LWin || e.Key == Keys.RWin)
                return;

            string comb = "";
            if ((e.Modifiers & HotKeyHelper.ModifierKeys.Windows) != 0) comb += "Win+";
            if ((e.Modifiers & HotKeyHelper.ModifierKeys.Control) != 0) comb += "Control+";
            if ((e.Modifiers & HotKeyHelper.ModifierKeys.Alt) != 0) comb += "Alt+";
            if ((e.Modifiers & HotKeyHelper.ModifierKeys.Shift) != 0) comb += "Shift+";

            comb += e.Key.ToString();

            ResultCombination = comb;
            e.Handled = true;

            this.Invoke(new Action(() => { lblKeys.Text = comb; }));
        }
    }
}