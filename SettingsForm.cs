using System;
using System.Windows.Forms;
using System.Drawing;
using System.Linq;

namespace HotKeyHelper
{
    public partial class SettingsForm : Form
    {
        private AppConfig _config;
        private KeyboardHook _hook;
        private FlowLayoutPanel _pnlActions;
        private CheckBox _chkStartup;

        public event EventHandler<AppConfig>? ConfigSaved;

        private class ActionTypeItem
        {
            public string Id { get; set; } = "";
            public string Name { get; set; } = "";
            public override string ToString() => Name;
        }

        public SettingsForm(AppConfig config, KeyboardHook hook)
        {
            _config = config;
            _hook = hook;
            InitializeComponent();
            LoadActions();
        }

        private void InitializeComponent()
        {
            this.Text = "Настройки МГК";
            this.Width = 900;
            this.Height = 700;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(30, 30, 30);
            this.ForeColor = Color.White;
            this.Font = new Font("Segoe UI", 10);

            var topPanel = new Panel { Dock = DockStyle.Top, Height = 60, Padding = new Padding(10) };
            
            _chkStartup = new CheckBox
            {
                Text = "Запускать приложение при старте Windows",
                AutoSize = true,
                Checked = _config.RunOnStartup,
                ForeColor = Color.White,
                Cursor = Cursors.Hand,
                Dock = DockStyle.Left,
                Padding = new Padding(0, 5, 0, 0)
            };
            
            var btnAdd = new Button
            {
                Text = "+ Добавить действие",
                Width = 200,
                Dock = DockStyle.Right,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(40, 167, 69),
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            btnAdd.FlatAppearance.BorderSize = 0;
            btnAdd.Click += (s, e) => 
            {
                var newAction = new HotkeyAction();
                _config.Hotkeys.Add(newAction);
                _pnlActions.Controls.Add(CreateActionCard(newAction));
            };

            topPanel.Controls.Add(_chkStartup);
            topPanel.Controls.Add(btnAdd);

            _pnlActions = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(10),
                BackColor = Color.FromArgb(45, 45, 48)
            };
            
            _pnlActions.SizeChanged += (s, e) => {
                foreach (Control ctrl in _pnlActions.Controls)
                {
                    ctrl.Width = _pnlActions.ClientSize.Width - 25;
                }
            };

            var bottomPanel = new Panel { Dock = DockStyle.Bottom, Height = 60, Padding = new Padding(10) };
            var btnSave = new Button
            {
                Text = "Сохранить и применить",
                Dock = DockStyle.Right,
                Width = 250,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            btnSave.FlatAppearance.BorderSize = 0;
            bottomPanel.Controls.Add(btnSave);

            btnSave.Click += (s, e) =>
            {
                _config.RunOnStartup = _chkStartup.Checked;
                ConfigSaved?.Invoke(this, _config);
                MessageBox.Show("Настройки успешно сохранены!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.Close();
            };

            this.Controls.Add(_pnlActions);
            this.Controls.Add(topPanel);
            this.Controls.Add(bottomPanel);
        }

        private void LoadActions()
        {
            _pnlActions.Controls.Clear();
            foreach (var action in _config.Hotkeys)
            {
                _pnlActions.Controls.Add(CreateActionCard(action));
            }
        }

        private Panel CreateActionCard(HotkeyAction action)
        {
            var card = new Panel
            {
                Width = _pnlActions.ClientSize.Width > 0 ? _pnlActions.ClientSize.Width - 25 : 850,
                Height = 160,
                BackColor = Color.FromArgb(60, 60, 65),
                Margin = new Padding(0, 0, 0, 10),
                Padding = new Padding(10)
            };

            int yOffset = 15;
            int col1 = 15;
            int col2 = 265;
            int col3 = 540;

            // Row 1: Name and Hotkey
            var lblName = new Label { Text = "Имя:", Location = new Point(col1, yOffset), AutoSize = true };
            var txtName = new TextBox { Text = action.Name, Location = new Point(col1, yOffset + 25), Width = 230, BackColor = Color.FromArgb(30, 30, 30), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
            txtName.TextChanged += (s, e) => action.Name = txtName.Text;

            var lblKey = new Label { Text = "Горячая клавиша:", Location = new Point(col2, yOffset), AutoSize = true };
            var btnKey = new Button { Text = string.IsNullOrEmpty(action.KeyCombination) ? "[Назначить]" : action.KeyCombination, Location = new Point(col2, yOffset + 25), Width = 230, Height = 27, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(80, 80, 85), Cursor = Cursors.Hand };
            btnKey.Click += (s, e) => {
                using (var kcf = new KeyCatchForm(action.KeyCombination, _hook))
                {
                    if (kcf.ShowDialog() == DialogResult.OK)
                    {
                        action.KeyCombination = kcf.ResultCombination;
                        btnKey.Text = string.IsNullOrEmpty(action.KeyCombination) ? "[Назначить]" : action.KeyCombination;
                    }
                }
            };

            var btnDelete = new Button { Text = "Удалить", Location = new Point(card.Width - 110, yOffset + 25), Width = 90, Height = 27, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(204, 51, 51), ForeColor = Color.White, Cursor = Cursors.Hand, Anchor = AnchorStyles.Top | AnchorStyles.Right };
            btnDelete.FlatAppearance.BorderSize = 0;
            btnDelete.Click += (s, e) => {
                _config.Hotkeys.Remove(action);
                card.Dispose();
            };

            yOffset += 70;

            // Row 2: Action Type, Target Path, Arguments
            var lblType = new Label { Text = "Действие:", Location = new Point(col1, yOffset), AutoSize = true };
            var cmbType = new ComboBox { Location = new Point(col1, yOffset + 25), Width = 230, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Color.FromArgb(30, 30, 30), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            
            var actionTypes = new[] {
                new ActionTypeItem { Id = "RunProgram", Name = "Запустить программу" },
                new ActionTypeItem { Id = "OpenFolder", Name = "Открыть папку" },
                new ActionTypeItem { Id = "CloseWindow", Name = "Закрыть активное окно (Мягко)" },
                new ActionTypeItem { Id = "KillProcess", Name = "Завершить процесс окна (Принудительно)" },
                new ActionTypeItem { Id = "ToggleMaximize", Name = "Развернуть / Восстановить окно" },
                new ActionTypeItem { Id = "MinimizeWindow", Name = "Свернуть окно" },
                new ActionTypeItem { Id = "LockScreen", Name = "Заблокировать экран Windows" },
                new ActionTypeItem { Id = "VolumeUp", Name = "Громкость +" },
                new ActionTypeItem { Id = "VolumeDown", Name = "Громкость -" },
                new ActionTypeItem { Id = "VolumeMute", Name = "Звук: Вкл/Выкл" },
                new ActionTypeItem { Id = "MediaPlayPause", Name = "Медиа: Пауза / Воспроизведение" }
            };
            cmbType.Items.AddRange(actionTypes);
            cmbType.SelectedItem = actionTypes.FirstOrDefault(x => x.Id == action.ActionType) ?? actionTypes[0];

            var lblPath = new Label { Text = "Путь/Команда:", Location = new Point(col2, yOffset), AutoSize = true };
            var txtPath = new TextBox { Text = action.TargetPath, Location = new Point(col2, yOffset + 25), Width = 190, BackColor = Color.FromArgb(30, 30, 30), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
            txtPath.TextChanged += (s, e) => action.TargetPath = txtPath.Text;

            var btnBrowse = new Button { Text = "...", Location = new Point(col2 + 195, yOffset + 24), Width = 35, Height = 28, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(80, 80, 85), Cursor = Cursors.Hand };
            btnBrowse.Click += (s, e) => {
                if (action.ActionType == "RunProgram")
                {
                    using (var ofd = new OpenFileDialog { Title = "Выберите программу", Filter = "Programs (*.exe;*.bat;*.cmd)|*.exe;*.bat;*.cmd|All files (*.*)|*.*" })
                    {
                        if (ofd.ShowDialog() == DialogResult.OK) { txtPath.Text = ofd.FileName; }
                    }
                }
                else if (action.ActionType == "OpenFolder")
                {
                    using (var fbd = new FolderBrowserDialog { Description = "Выберите папку" })
                    {
                        if (fbd.ShowDialog() == DialogResult.OK) { txtPath.Text = fbd.SelectedPath; }
                    }
                }
            };

            var lblArgs = new Label { Text = "Аргументы:", Location = new Point(col3, yOffset), AutoSize = true };
            var txtArgs = new TextBox { Text = action.Arguments, Location = new Point(col3, yOffset + 25), Width = card.Width - col3 - 20, BackColor = Color.FromArgb(30, 30, 30), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            txtArgs.TextChanged += (s, e) => action.Arguments = txtArgs.Text;

            Action updateVisibility = () => {
                var selected = cmbType.SelectedItem as ActionTypeItem;
                if (selected != null) {
                    action.ActionType = selected.Id;
                }
                string type = action.ActionType;
                
                bool needsPath = (type == "RunProgram" || type == "OpenFolder");
                bool needsArgs = (type == "RunProgram");

                lblPath.Visible = txtPath.Visible = btnBrowse.Visible = needsPath;
                lblArgs.Visible = txtArgs.Visible = needsArgs;
            };

            cmbType.SelectedIndexChanged += (s, e) => updateVisibility();
            updateVisibility();

            card.Controls.Add(lblName); card.Controls.Add(txtName);
            card.Controls.Add(lblKey); card.Controls.Add(btnKey);
            card.Controls.Add(btnDelete);
            card.Controls.Add(lblType); card.Controls.Add(cmbType);
            card.Controls.Add(lblPath); card.Controls.Add(txtPath); card.Controls.Add(btnBrowse);
            card.Controls.Add(lblArgs); card.Controls.Add(txtArgs);

            return card;
        }
    }
}