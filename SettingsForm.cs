using System.Windows.Forms;
using DVDify.Models;

namespace DVDify;

public class SettingsForm : Form
{
    private AppConfig _config;
    private DataGridView _rulesGrid = null!;
    private TextBox _hotkeyTextBox = null!;
    private NumericUpDown _speedNumeric = null!;
    private NumericUpDown _intervalNumeric = null!;
    private NumericUpDown _maxSizePercentNumeric = null!;
    private CheckBox _useAllScreensCheckBox = null!;
    private CheckBox _debugLoggingCheckBox = null!;
    private CheckBox _confettiEnabledCheckBox = null!;
    private NumericUpDown _confettiParticleCountNumeric = null!;
    private NumericUpDown _confettiDurationNumeric = null!;
    private NumericUpDown _confettiMarginPercentNumeric = null!;
    private Button _saveButton = null!;
    private Button _testHotkeyButton = null!;
    private bool _capturingHotkey = false;

    public event EventHandler<AppConfig>? ConfigSaved;

    public SettingsForm(AppConfig config)
    {
        _config = new AppConfig
        {
            Hotkey = new HotkeyConfig
            {
                WinKey = config.Hotkey.WinKey,
                CtrlKey = config.Hotkey.CtrlKey,
                AltKey = config.Hotkey.AltKey,
                ShiftKey = config.Hotkey.ShiftKey,
                KeyCode = config.Hotkey.KeyCode
            },
            Animation = new AnimationConfig
            {
                Speed = config.Animation.Speed,
                UpdateInterval = config.Animation.UpdateInterval,
                MaxWindowSizePercent = config.Animation.MaxWindowSizePercent,
                UseAllScreens = config.Animation.UseAllScreens
            },
            Confetti = new ConfettiConfig
            {
                Enabled = config.Confetti.Enabled,
                ParticleCount = config.Confetti.ParticleCount,
                DurationFrames = config.Confetti.DurationFrames,
                PerfectHitMarginPercent = config.Confetti.PerfectHitMarginPercent
            },
            WindowRules = config.WindowRules.Select(r => new WindowRule
            {
                WindowName = r.WindowName,
                ClassName = r.ClassName,
                ExecutablePath = r.ExecutablePath,
                Enabled = r.Enabled
            }).ToList(),
            DebugLogging = config.DebugLogging
        };

        InitializeComponent();
        LoadConfig();
    }

    private void InitializeComponent()
    {
        Text = "DVDify Settings";
        Size = new Size(800, 600);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        var mainPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(10),
            ColumnCount = 2,
            RowCount = 8
        };
        mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        // Hotkey section
        mainPanel.Controls.Add(new Label { Text = "Hotkey:", Anchor = AnchorStyles.Left | AnchorStyles.Right }, 0, 0);
        var hotkeyPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true };
        _hotkeyTextBox = new TextBox { Width = 200, ReadOnly = true };
        _testHotkeyButton = new Button { Text = "Set Hotkey", AutoSize = true };
        _testHotkeyButton.Click += TestHotkeyButton_Click;
        hotkeyPanel.Controls.Add(_hotkeyTextBox);
        hotkeyPanel.Controls.Add(_testHotkeyButton);
        mainPanel.Controls.Add(hotkeyPanel, 1, 0);

        // Animation Speed
        mainPanel.Controls.Add(new Label { Text = "Animation Speed:", Anchor = AnchorStyles.Left | AnchorStyles.Right }, 0, 1);
        _speedNumeric = new NumericUpDown { Minimum = 1, Maximum = 50, Value = _config.Animation.Speed, Dock = DockStyle.Fill };
        mainPanel.Controls.Add(_speedNumeric, 1, 1);

        // Update Interval
        mainPanel.Controls.Add(new Label { Text = "Update Interval (ms):", Anchor = AnchorStyles.Left | AnchorStyles.Right }, 0, 2);
        _intervalNumeric = new NumericUpDown { Minimum = 1, Maximum = 100, Value = _config.Animation.UpdateInterval, Dock = DockStyle.Fill };
        mainPanel.Controls.Add(_intervalNumeric, 1, 2);

        // Max Window Size Percent
        mainPanel.Controls.Add(new Label { Text = "Max Window Size (%):", Anchor = AnchorStyles.Left | AnchorStyles.Right }, 0, 3);
        _maxSizePercentNumeric = new NumericUpDown { Minimum = 1, Maximum = 100, Value = _config.Animation.MaxWindowSizePercent, Dock = DockStyle.Fill };
        mainPanel.Controls.Add(_maxSizePercentNumeric, 1, 3);

        // Use All Screens
        mainPanel.Controls.Add(new Label { Text = "Use All Screens:", Anchor = AnchorStyles.Left | AnchorStyles.Right }, 0, 4);
        _useAllScreensCheckBox = new CheckBox { Checked = _config.Animation.UseAllScreens, Dock = DockStyle.Fill, Text = "Bounce across all screens (unchecked = current screen only)" };
        mainPanel.Controls.Add(_useAllScreensCheckBox, 1, 4);

        // Debug Logging
        mainPanel.Controls.Add(new Label { Text = "Debug Logging:", Anchor = AnchorStyles.Left | AnchorStyles.Right }, 0, 5);
        _debugLoggingCheckBox = new CheckBox { Checked = _config.DebugLogging, Dock = DockStyle.Fill, Text = "Enable debug logging to file" };
        mainPanel.Controls.Add(_debugLoggingCheckBox, 1, 5);

        // Confetti Settings
        var confettiLabel = new Label { Text = "Confetti Settings:", Anchor = AnchorStyles.Left, Font = new Font(DefaultFont, FontStyle.Bold) };
        mainPanel.SetColumnSpan(confettiLabel, 2);
        mainPanel.Controls.Add(confettiLabel, 0, 6);

        // Confetti Enabled
        mainPanel.Controls.Add(new Label { Text = "Enable Confetti:", Anchor = AnchorStyles.Left | AnchorStyles.Right }, 0, 7);
        _confettiEnabledCheckBox = new CheckBox { Checked = _config.Confetti.Enabled, Dock = DockStyle.Fill, Text = "Show confetti on perfect edge hits" };
        mainPanel.Controls.Add(_confettiEnabledCheckBox, 1, 7);

        // Confetti Particle Count
        mainPanel.Controls.Add(new Label { Text = "Particle Count:", Anchor = AnchorStyles.Left | AnchorStyles.Right }, 0, 8);
        _confettiParticleCountNumeric = new NumericUpDown { Minimum = 50, Maximum = 500, Value = _config.Confetti.ParticleCount, Dock = DockStyle.Fill };
        mainPanel.Controls.Add(_confettiParticleCountNumeric, 1, 8);

        // Confetti Duration
        mainPanel.Controls.Add(new Label { Text = "Duration (frames):", Anchor = AnchorStyles.Left | AnchorStyles.Right }, 0, 9);
        _confettiDurationNumeric = new NumericUpDown { Minimum = 30, Maximum = 180, Value = _config.Confetti.DurationFrames, Dock = DockStyle.Fill };
        mainPanel.Controls.Add(_confettiDurationNumeric, 1, 9);

        // Perfect Hit Margin
        mainPanel.Controls.Add(new Label { Text = "Perfect Hit Margin (%):", Anchor = AnchorStyles.Left | AnchorStyles.Right }, 0, 10);
        _confettiMarginPercentNumeric = new NumericUpDown { Minimum = 0.1m, Maximum = 5.0m, DecimalPlaces = 1, Value = (decimal)_config.Confetti.PerfectHitMarginPercent, Dock = DockStyle.Fill };
        mainPanel.Controls.Add(_confettiMarginPercentNumeric, 1, 10);

        // Window Rules section
        var rulesLabel = new Label { Text = "Window Rules:", Anchor = AnchorStyles.Left, Font = new Font(DefaultFont, FontStyle.Bold) };
        mainPanel.SetColumnSpan(rulesLabel, 2);
        mainPanel.Controls.Add(rulesLabel, 0, 11);

        _rulesGrid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            AllowUserToAddRows = true,
            AllowUserToDeleteRows = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            RowHeadersVisible = false
        };
        mainPanel.SetColumnSpan(_rulesGrid, 2);
        mainPanel.Controls.Add(_rulesGrid, 0, 7);

        _rulesGrid.Columns.Add(new DataGridViewCheckBoxColumn
        {
            Name = "Enabled",
            HeaderText = "Enabled",
            Width = 50,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None
        });
        _rulesGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "WindowName",
            HeaderText = "Window Name",
            Width = 200
        });
        _rulesGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "ClassName",
            HeaderText = "Class Name",
            Width = 200
        });
        _rulesGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "ExecutablePath",
            HeaderText = "Executable Path",
            Width = 300
        });

        // Buttons
        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(10)
        };
        _saveButton = new Button { Text = "Save", DialogResult = DialogResult.OK, Width = 75 };
        var cancelButton = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Width = 75 };
        var dumpWindowsButton = new Button { Text = "Dump All Windows", AutoSize = true };
        dumpWindowsButton.Click += DumpWindowsButton_Click;
        var testWindowButton = new Button { Text = "Spawn Test Window", AutoSize = true };
        testWindowButton.Click += TestWindowButton_Click;
        _saveButton.Click += SaveButton_Click;
        buttonPanel.Controls.Add(_saveButton);
        buttonPanel.Controls.Add(cancelButton);
        buttonPanel.Controls.Add(dumpWindowsButton);
        buttonPanel.Controls.Add(testWindowButton);

        Controls.Add(mainPanel);
        Controls.Add(buttonPanel);
    }

    private void LoadConfig()
    {
        UpdateHotkeyDisplay();
        _speedNumeric.Value = _config.Animation.Speed;
        _intervalNumeric.Value = _config.Animation.UpdateInterval;
        _maxSizePercentNumeric.Value = _config.Animation.MaxWindowSizePercent;
        _useAllScreensCheckBox.Checked = _config.Animation.UseAllScreens;
        _debugLoggingCheckBox.Checked = _config.DebugLogging;

        _rulesGrid.Rows.Clear();
        foreach (var rule in _config.WindowRules)
        {
            _rulesGrid.Rows.Add(rule.Enabled, rule.WindowName ?? "", rule.ClassName ?? "", rule.ExecutablePath ?? "");
        }
    }

    private void UpdateHotkeyDisplay()
    {
        var parts = new List<string>();
        if (_config.Hotkey.WinKey) parts.Add("Win");
        if (_config.Hotkey.CtrlKey) parts.Add("Ctrl");
        if (_config.Hotkey.AltKey) parts.Add("Alt");
        if (_config.Hotkey.ShiftKey) parts.Add("Shift");
        parts.Add(((Keys)_config.Hotkey.KeyCode).ToString());
        _hotkeyTextBox.Text = string.Join(" + ", parts);
    }

    private void TestHotkeyButton_Click(object? sender, EventArgs e)
    {
        _capturingHotkey = true;
        _testHotkeyButton.Text = "Press keys...";
        _testHotkeyButton.Enabled = false;
        KeyDown += SettingsForm_KeyDown;
        KeyPreview = true;
        Focus();
    }

    private void SettingsForm_KeyDown(object? sender, KeyEventArgs e)
    {
        if (!_capturingHotkey) return;

        e.Handled = true;
        e.SuppressKeyPress = true;

        _config.Hotkey.WinKey = e.Modifiers.HasFlag(Keys.LWin) || e.Modifiers.HasFlag(Keys.RWin);
        _config.Hotkey.CtrlKey = e.Modifiers.HasFlag(Keys.Control);
        _config.Hotkey.AltKey = e.Modifiers.HasFlag(Keys.Alt);
        _config.Hotkey.ShiftKey = e.Modifiers.HasFlag(Keys.Shift);

        // Get the base key without modifiers
        var key = e.KeyCode;
        if (key != Keys.LWin && key != Keys.RWin && key != Keys.ControlKey && 
            key != Keys.Menu && key != Keys.ShiftKey)
        {
            _config.Hotkey.KeyCode = (int)key;
            UpdateHotkeyDisplay();
            _capturingHotkey = false;
            _testHotkeyButton.Text = "Set Hotkey";
            _testHotkeyButton.Enabled = true;
            KeyDown -= SettingsForm_KeyDown;
        }
    }

    private void SaveButton_Click(object? sender, EventArgs e)
    {
        _config.Animation.Speed = (int)_speedNumeric.Value;
        _config.Animation.UpdateInterval = (int)_intervalNumeric.Value;
        _config.Animation.MaxWindowSizePercent = (int)_maxSizePercentNumeric.Value;
        _config.Animation.UseAllScreens = _useAllScreensCheckBox.Checked;
        _config.DebugLogging = _debugLoggingCheckBox.Checked;
        
        _config.Confetti.Enabled = _confettiEnabledCheckBox.Checked;
        _config.Confetti.ParticleCount = (int)_confettiParticleCountNumeric.Value;
        _config.Confetti.DurationFrames = (int)_confettiDurationNumeric.Value;
        _config.Confetti.PerfectHitMarginPercent = (double)_confettiMarginPercentNumeric.Value;

        _config.WindowRules.Clear();
        foreach (DataGridViewRow row in _rulesGrid.Rows)
        {
            if (row.IsNewRow) continue;

            var rule = new WindowRule
            {
                Enabled = row.Cells["Enabled"].Value as bool? ?? true,
                WindowName = row.Cells["WindowName"].Value?.ToString(),
                ClassName = row.Cells["ClassName"].Value?.ToString(),
                ExecutablePath = row.Cells["ExecutablePath"].Value?.ToString()
            };

            // Only add if at least one field is filled
            if (!string.IsNullOrWhiteSpace(rule.WindowName) ||
                !string.IsNullOrWhiteSpace(rule.ClassName) ||
                !string.IsNullOrWhiteSpace(rule.ExecutablePath))
            {
                _config.WindowRules.Add(rule);
            }
        }

        ConfigSaved?.Invoke(this, _config);
        Close();
    }

    private void DumpWindowsButton_Click(object? sender, EventArgs e)
    {
        try
        {
            Cursor = Cursors.WaitCursor;
            var tempFile = WindowUtils.DumpAllWindowsToFile();
            
            // Open the file with the default text editor
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = tempFile,
                UseShellExecute = true
            });

            MessageBox.Show(
                $"Windows info dumped to:\n{tempFile}\n\nThe file has been opened for you to copy/paste the relevant data.",
                "Windows Dump Complete",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Error dumping windows info:\n{ex.Message}",
                "Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            Cursor = Cursors.Default;
        }
    }

    private void TestWindowButton_Click(object? sender, EventArgs e)
    {
        var testWindow = new TestWindow();
        // The window will show itself in its constructor
    }
}
