using System.Windows.Forms;
using DVDify.Models;

namespace DVDify;

public class TrayApplicationContext : ApplicationContext
{
    private NotifyIcon? _notifyIcon;
    private SettingsForm? _settingsForm;
    private HotkeyManager? _hotkeyManager;
    private WindowBouncer? _windowBouncer;
    private AppConfig _config;

    public TrayApplicationContext()
    {
        _config = ConfigManager.Load();
        
        // Enable debug logging if configured
        if (_config.DebugLogging)
        {
            DebugLogger.Enable();
        }
        
        InitializeTrayIcon();
        InitializeHotkey();
        InitializeBouncer();
        
        // Check for matching windows on startup and start animation if found
        CheckAndStartOnStartup();
        
        // Start periodic window watcher to catch new windows
        StartWindowWatcher();
    }
    
    private void CheckAndStartOnStartup()
    {
        // Use a timer to delay the check slightly to ensure everything is initialized
        var startupTimer = new System.Windows.Forms.Timer
        {
            Interval = 500, // Wait 500ms after startup
            Enabled = true
        };
        
        startupTimer.Tick += (sender, e) =>
        {
            startupTimer.Stop();
            startupTimer.Dispose();
            
            if (_windowBouncer == null)
                return;
            
            DebugLogger.Log("Startup: Checking for matching windows...");
            
            // Check if there's a matching window
            var allWindows = WindowUtils.GetAllWindowsInfo();
            DebugLogger.Log($"Startup: Found {allWindows.Count} visible windows");
            
            // Find all matching windows and pick the best one (largest with title preferred)
            WindowInfo? bestMatch = null;
            int bestMatchScore = 0;
            
            foreach (var window in allWindows)
            {
                if (MatchesAnyRule(window))
                {
                    // Score windows: prefer larger windows (likely main windows)
                    // and windows with titles (more likely to be important)
                    int score = window.Width * window.Height;
                    if (!string.IsNullOrEmpty(window.WindowName))
                        score += 1000000; // Bonus for having a title
                    
                    DebugLogger.Log($"Startup: Found matching window - Score: {score}, Title: '{window.WindowName}', Class: '{window.ClassName}'");
                    
                    if (score > bestMatchScore)
                    {
                        bestMatch = window;
                        bestMatchScore = score;
                    }
                }
            }
            
            if (bestMatch != null)
            {
                DebugLogger.Log($"Startup: Starting animation with best match (score: {bestMatchScore})");
                WindowUtils.BringWindowToForeground(bestMatch.Handle);
                System.Threading.Thread.Sleep(100);
                _windowBouncer.Start(bestMatch);
            }
            else
            {
                DebugLogger.Log("Startup: No matching windows found");
            }
        };
    }
    
    private void StartWindowWatcher()
    {
        // Check for new matching windows every 2 seconds when not bouncing
        _windowWatcherTimer = new System.Windows.Forms.Timer
        {
            Interval = 2000, // Check every 2 seconds
            Enabled = true
        };
        
        _windowWatcherTimer.Tick += (sender, e) =>
        {
            // Only check if not currently bouncing
            if (_windowBouncer == null || _windowBouncer.IsRunning)
                return;
            
            // Find all matching windows and pick the best one
            var allWindows = WindowUtils.GetAllWindowsInfo();
            WindowInfo? bestMatch = null;
            int bestMatchScore = 0;
            
            foreach (var window in allWindows)
            {
                if (MatchesAnyRule(window))
                {
                    // Score windows: prefer larger windows (likely main windows)
                    // and windows with titles (more likely to be important)
                    int score = window.Width * window.Height;
                    if (!string.IsNullOrEmpty(window.WindowName))
                        score += 1000000; // Bonus for having a title
                    
                    if (score > bestMatchScore)
                    {
                        bestMatch = window;
                        bestMatchScore = score;
                    }
                }
            }
            
            if (bestMatch != null)
            {
                DebugLogger.Log($"WindowWatcher: Found new matching window, starting animation");
                WindowUtils.BringWindowToForeground(bestMatch.Handle);
                System.Threading.Thread.Sleep(100);
                _windowBouncer.Start(bestMatch);
            }
        };
    }
    
    private bool MatchesAnyRule(WindowInfo windowInfo)
    {
        if (_config.WindowRules == null || _config.WindowRules.Count == 0)
            return true;

        foreach (var rule in _config.WindowRules)
        {
            if (!rule.Enabled)
                continue;

            bool hasAnyField = false;
            bool matches = true;

            if (!string.IsNullOrWhiteSpace(rule.WindowName))
            {
                hasAnyField = true;
                matches &= GlobMatch(rule.WindowName, windowInfo.WindowName);
            }

            if (!string.IsNullOrWhiteSpace(rule.ClassName))
            {
                hasAnyField = true;
                matches &= GlobMatch(rule.ClassName, windowInfo.ClassName);
            }

            if (!string.IsNullOrWhiteSpace(rule.ExecutablePath))
            {
                hasAnyField = true;
                
                string exePathToMatch = windowInfo.ExecutablePath;
                if (!rule.ExecutablePath.Contains('\\') && !rule.ExecutablePath.Contains('/'))
                {
                    if (!string.IsNullOrEmpty(windowInfo.ExecutablePath))
                    {
                        exePathToMatch = Path.GetFileName(windowInfo.ExecutablePath);
                    }
                }
                
                matches &= GlobMatch(rule.ExecutablePath, exePathToMatch);
            }

            if (hasAnyField && matches)
                return true;
        }

        return false;
    }
    
    private bool GlobMatch(string pattern, string text)
    {
        if (string.IsNullOrEmpty(pattern))
            return true;

        if (string.IsNullOrEmpty(text))
            return false;

        pattern = pattern.ToLowerInvariant();
        text = text.ToLowerInvariant();

        int patternIndex = 0;
        int textIndex = 0;
        int patternStar = -1;
        int textStar = -1;

        while (textIndex < text.Length)
        {
            if (patternIndex < pattern.Length && (pattern[patternIndex] == '?' || pattern[patternIndex] == text[textIndex]))
            {
                patternIndex++;
                textIndex++;
            }
            else if (patternIndex < pattern.Length && pattern[patternIndex] == '*')
            {
                patternStar = patternIndex;
                textStar = textIndex;
                patternIndex++;
            }
            else if (patternStar != -1)
            {
                patternIndex = patternStar + 1;
                textIndex = textStar + 1;
                textStar++;
            }
            else
            {
                return false;
            }
        }

        while (patternIndex < pattern.Length && pattern[patternIndex] == '*')
        {
            patternIndex++;
        }

        return patternIndex == pattern.Length;
    }

    private void InitializeTrayIcon()
    {
        _notifyIcon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = "DVDify",
            Visible = true,
            ContextMenuStrip = new ContextMenuStrip()
        };

        var settingsMenuItem = new ToolStripMenuItem("Settings", null, SettingsMenuItem_Click);
        var exitMenuItem = new ToolStripMenuItem("Exit", null, ExitMenuItem_Click);

        _notifyIcon.ContextMenuStrip.Items.Add(settingsMenuItem);
        _notifyIcon.ContextMenuStrip.Items.Add(new ToolStripSeparator());
        _notifyIcon.ContextMenuStrip.Items.Add(exitMenuItem);

        _notifyIcon.DoubleClick += SettingsMenuItem_Click;
    }

    private HotkeyMessageFilter? _messageFilter;
    private System.Windows.Forms.Timer? _windowWatcherTimer;

    private void InitializeHotkey()
    {
        _messageFilter = new HotkeyMessageFilter();

        _hotkeyManager = new HotkeyManager(_messageFilter.Handle, _config.Hotkey);
        _hotkeyManager.HotkeyPressed += HotkeyManager_HotkeyPressed;
        
        if (!_hotkeyManager.Register())
        {
            // Hotkey registration failed - show notification
            _notifyIcon?.ShowBalloonTip(5000, "DVDify", 
                "Failed to register hotkey. It may already be in use.", 
                ToolTipIcon.Warning);
        }

        _messageFilter.HotkeyManager = _hotkeyManager;
    }

    private void InitializeBouncer()
    {
        _windowBouncer = new WindowBouncer(_config);
    }

    private void HotkeyManager_HotkeyPressed(object? sender, EventArgs e)
    {
        _windowBouncer?.Start();
    }

    private void SettingsMenuItem_Click(object? sender, EventArgs e)
    {
        if (_settingsForm == null || _settingsForm.IsDisposed)
        {
            _settingsForm = new SettingsForm(_config);
            _settingsForm.ConfigSaved += SettingsForm_ConfigSaved;
            _settingsForm.Show();
        }
        else
        {
            _settingsForm.BringToFront();
            _settingsForm.Activate();
        }
    }

    private void SettingsForm_ConfigSaved(object? sender, AppConfig config)
    {
        _config = config;
        ConfigManager.Save(_config);
        _hotkeyManager?.UpdateConfig(_config.Hotkey);
        _windowBouncer?.UpdateConfig(_config);
        
        // Update debug logging
        if (_config.DebugLogging)
        {
            DebugLogger.Enable();
        }
        else
        {
            DebugLogger.Disable();
        }
    }

    private void ExitMenuItem_Click(object? sender, EventArgs e)
    {
        _notifyIcon?.Dispose();
        _hotkeyManager?.Dispose();
        _windowBouncer?.Stop();
        _messageFilter?.Dispose();
        _windowWatcherTimer?.Stop();
        _windowWatcherTimer?.Dispose();
        Application.Exit();
    }

    protected override void ExitThreadCore()
    {
        _notifyIcon?.Dispose();
        _hotkeyManager?.Dispose();
        _windowBouncer?.Stop();
        _messageFilter?.Dispose();
        _windowWatcherTimer?.Stop();
        _windowWatcherTimer?.Dispose();
        base.ExitThreadCore();
    }
}

public class HotkeyMessageFilter : Form, IDisposable
{
    public HotkeyManager? HotkeyManager { get; set; }

    public HotkeyMessageFilter()
    {
        // Create a hidden window to receive hotkey messages
        WindowState = FormWindowState.Minimized;
        ShowInTaskbar = false;
        FormBorderStyle = FormBorderStyle.None;
        Size = new Size(0, 0);
        ShowIcon = false;
        Load += (s, e) => Hide();
        CreateControl();
        Show();
        Hide();
    }

    protected override void WndProc(ref Message m)
    {
        const int WM_HOTKEY = 0x0312;
        if (m.Msg == WM_HOTKEY)
        {
            HotkeyManager?.ProcessMessage(m);
        }
        base.WndProc(ref m);
    }
}
