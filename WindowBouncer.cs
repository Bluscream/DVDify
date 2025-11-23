using System.Linq;
using System.Windows.Forms;
using DVDify.Models;

namespace DVDify;

public class WindowBouncer
{
    private bool _isRunning = false;
    private WindowInfo? _currentWindow;
    private int _velocityX;
    private int _velocityY;
    private System.Windows.Forms.Timer? _animationTimer;
    private Rectangle _allScreensBounds;
    private AppConfig _config;

    public event EventHandler? BouncingStopped;

    public WindowBouncer(AppConfig config)
    {
        _config = config;
    }

    public bool IsRunning => _isRunning;

    public void Start(WindowInfo? specificWindow = null)
    {
        // If already running and hotkey pressed (specificWindow is null), stop and restart
        if (_isRunning && specificWindow == null)
        {
            DebugLogger.Log("Bouncing stopped (already running, hotkey pressed again)");
            Stop();
            // Small delay to ensure window state is reset
            System.Threading.Thread.Sleep(50);
            // Continue to start with new window
        }
        else if (_isRunning && specificWindow != null)
        {
            // Already running with a specific window, don't restart
            DebugLogger.Log("Bouncing already running, ignoring new window");
            return;
        }

        DebugLogger.Log("=== Starting bounce ===");
        DebugLogger.Log($"Total rules: {_config.WindowRules?.Count ?? 0}, Enabled rules: {_config.WindowRules?.Count(r => r.Enabled) ?? 0}");

        WindowInfo? windowInfo = specificWindow;

        if (windowInfo == null)
        {
            // First try the foreground window
            var foregroundWindow = WindowUtils.GetForegroundWindowInfo();
            if (foregroundWindow.Handle != IntPtr.Zero)
            {
                DebugLogger.LogWindowInfo("Foreground window", foregroundWindow);
                if (MatchesAnyRule(foregroundWindow))
                {
                    DebugLogger.Log("Foreground window matches a rule");
                    windowInfo = foregroundWindow;
                }
                else
                {
                    DebugLogger.Log("Foreground window does NOT match any rule");
                }
            }
            else
            {
                DebugLogger.Log("No foreground window found");
            }

            if (windowInfo == null)
            {
                // If foreground window doesn't match, search all windows for a match
                DebugLogger.Log("Searching all windows for a match...");
                var allWindows = WindowUtils.GetAllWindowsInfo();
                DebugLogger.Log($"Found {allWindows.Count} visible windows");
                
                foreach (var window in allWindows)
                {
                    if (MatchesAnyRule(window))
                    {
                        DebugLogger.LogWindowInfo("Found matching window", window);
                        windowInfo = window;
                        // Bring the matched window to foreground
                        WindowUtils.BringWindowToForeground(window.Handle);
                        break;
                    }
                }

                if (windowInfo == null)
                {
                    DebugLogger.Log("No matching window found in all windows");
                }
            }
        }
        else
        {
            DebugLogger.LogWindowInfo("Using provided window", windowInfo);
            if (!MatchesAnyRule(windowInfo))
            {
                DebugLogger.Log("Provided window does NOT match any rule");
                windowInfo = null;
            }
        }

        if (windowInfo == null || windowInfo.Handle == IntPtr.Zero)
        {
            DebugLogger.Log("No valid window to bounce - exiting");
            return;
        }

        // Normalize and resize window if needed
        WindowUtils.NormalizeAndResizeWindow(windowInfo.Handle, _config.Animation.MaxWindowSizePercent);
        
        // Refresh window info after normalization (unless we were given a specific window)
        if (specificWindow == null)
        {
            System.Threading.Thread.Sleep(100); // Give window time to normalize
            var refreshedWindow = WindowUtils.GetForegroundWindowInfo();
            if (refreshedWindow.Handle != IntPtr.Zero)
            {
                windowInfo = refreshedWindow;
            }
        }
        else
        {
            // Refresh the provided window's position/size after normalization
            System.Threading.Thread.Sleep(100);
            var refreshedWindow = WindowUtils.GetWindowInfo(windowInfo.Handle);
            if (refreshedWindow.Handle != IntPtr.Zero)
            {
                windowInfo = refreshedWindow;
            }
        }
        
        if (windowInfo.Handle == IntPtr.Zero)
            return;

        _currentWindow = windowInfo;
        
        // Get bounds based on configuration
        if (_config.Animation.UseAllScreens)
        {
            _allScreensBounds = WindowUtils.GetAllScreensBounds();
        }
        else
        {
            _allScreensBounds = WindowUtils.GetCurrentScreenBounds(windowInfo.Handle);
        }

        // Initialize velocity (diagonal movement)
        _velocityX = _config.Animation.Speed;
        _velocityY = _config.Animation.Speed;

        _isRunning = true;

        // Start animation timer
        _animationTimer = new System.Windows.Forms.Timer
        {
            Interval = _config.Animation.UpdateInterval
        };
        _animationTimer.Tick += AnimationTimer_Tick;
        _animationTimer.Start();
    }

    public void Stop()
    {
        if (!_isRunning)
            return;

        _isRunning = false;
        _animationTimer?.Stop();
        _animationTimer?.Dispose();
        _animationTimer = null;
        _currentWindow = null;

        BouncingStopped?.Invoke(this, EventArgs.Empty);
    }

    private void AnimationTimer_Tick(object? sender, EventArgs e)
    {
        if (!_isRunning || _currentWindow == null)
        {
            Stop();
            return;
        }

        // Calculate new position
        int newX = _currentWindow.X + _velocityX;
        int newY = _currentWindow.Y + _velocityY;

        // Check boundaries and bounce
        if (newX <= _allScreensBounds.Left)
        {
            newX = _allScreensBounds.Left;
            _velocityX = Math.Abs(_velocityX); // Bounce right
        }
        else if (newX + _currentWindow.Width >= _allScreensBounds.Right)
        {
            newX = _allScreensBounds.Right - _currentWindow.Width;
            _velocityX = -Math.Abs(_velocityX); // Bounce left
        }

        if (newY <= _allScreensBounds.Top)
        {
            newY = _allScreensBounds.Top;
            _velocityY = Math.Abs(_velocityY); // Bounce down
        }
        else if (newY + _currentWindow.Height >= _allScreensBounds.Bottom)
        {
            newY = _allScreensBounds.Bottom - _currentWindow.Height;
            _velocityY = -Math.Abs(_velocityY); // Bounce up
        }

        // Update window position
        WindowUtils.SetWindowPosition(_currentWindow.Handle, newX, newY);
        _currentWindow.X = newX;
        _currentWindow.Y = newY;
    }

    private bool MatchesAnyRule(WindowInfo windowInfo)
    {
        if (_config.WindowRules == null || _config.WindowRules.Count == 0)
        {
            DebugLogger.Log("No rules configured - all windows match");
            return true; // No rules means all windows match
        }

        // Check if any enabled rule matches
        int ruleIndex = 0;
        foreach (var rule in _config.WindowRules)
        {
            ruleIndex++;
            if (!rule.Enabled)
            {
                DebugLogger.Log($"Rule {ruleIndex}: Disabled, skipping");
                continue;
            }

            DebugLogger.Log($"Rule {ruleIndex}: Checking WindowName='{rule.WindowName}', ClassName='{rule.ClassName}', ExecutablePath='{rule.ExecutablePath}'");

            // A rule matches if at least one specified field matches
            // If all fields are empty, the rule doesn't match anything
            bool hasAnyField = false;
            bool matches = true;

            if (!string.IsNullOrWhiteSpace(rule.WindowName))
            {
                hasAnyField = true;
                bool nameMatch = GlobMatch(rule.WindowName, windowInfo.WindowName);
                matches &= nameMatch;
                DebugLogger.Log($"  WindowName match: '{rule.WindowName}' vs '{windowInfo.WindowName}' = {nameMatch}");
            }

            if (!string.IsNullOrWhiteSpace(rule.ClassName))
            {
                hasAnyField = true;
                bool classMatch = GlobMatch(rule.ClassName, windowInfo.ClassName);
                matches &= classMatch;
                DebugLogger.Log($"  ClassName match: '{rule.ClassName}' vs '{windowInfo.ClassName}' = {classMatch}");
            }

            if (!string.IsNullOrWhiteSpace(rule.ExecutablePath))
            {
                hasAnyField = true;
                
                // If the pattern doesn't contain path separators, compare against filename only
                string exePathToMatch = windowInfo.ExecutablePath;
                if (!rule.ExecutablePath.Contains('\\') && !rule.ExecutablePath.Contains('/'))
                {
                    // Pattern is just a filename, extract filename from full path for comparison
                    if (!string.IsNullOrEmpty(windowInfo.ExecutablePath))
                    {
                        exePathToMatch = Path.GetFileName(windowInfo.ExecutablePath);
                    }
                }
                
                bool exeMatch = GlobMatch(rule.ExecutablePath, exePathToMatch);
                matches &= exeMatch;
                DebugLogger.Log($"  ExecutablePath match: '{rule.ExecutablePath}' vs '{exePathToMatch}' (from '{windowInfo.ExecutablePath}') = {exeMatch}");
            }

            // Only consider the rule if it has at least one field and all specified fields match
            if (hasAnyField && matches)
            {
                DebugLogger.Log($"Rule {ruleIndex}: MATCH!");
                return true;
            }
            else if (hasAnyField)
            {
                DebugLogger.Log($"Rule {ruleIndex}: No match (hasAnyField={hasAnyField}, matches={matches})");
            }
            else
            {
                DebugLogger.Log($"Rule {ruleIndex}: No match (all fields empty)");
            }
        }

        DebugLogger.Log("No rules matched");
        return false;
    }

    private bool GlobMatch(string pattern, string text)
    {
        if (string.IsNullOrEmpty(pattern))
            return true;

        if (string.IsNullOrEmpty(text))
            return false;

        // Case-insensitive comparison
        pattern = pattern.ToLowerInvariant();
        text = text.ToLowerInvariant();

        // Simple glob matching: * matches any sequence, ? matches any character
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

    public void UpdateConfig(AppConfig config)
    {
        _config = config;
        if (_animationTimer != null)
        {
            _animationTimer.Interval = _config.Animation.UpdateInterval;
        }
        
        if (_config.DebugLogging)
        {
            DebugLogger.Enable();
        }
        else
        {
            DebugLogger.Disable();
        }
    }
}
