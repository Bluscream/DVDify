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
        // If already running and hotkey pressed (specificWindow is null), stop
        if (_isRunning && specificWindow == null)
        {
            DebugLogger.Log("Bouncing stopped (already running, hotkey pressed again - manual stop)");
            Stop();
            // Don't restart - user manually stopped it
            return;
        }
        else if (_isRunning && specificWindow != null)
        {
            // Already running with a specific window, don't restart
            DebugLogger.Log("Bouncing already running, ignoring new window");
            return;
        }

        DebugLogger.Log("=== Starting bounce ===");
        int totalRules = _config.WindowRules?.Count ?? 0;
        int enabledRules = _config.WindowRules?.Count(r => r.Enabled) ?? 0;
        DebugLogger.Log($"Total rules: {totalRules}, Enabled rules: {enabledRules}");

        WindowInfo? windowInfo = specificWindow;

        if (windowInfo == null)
        {
            // Hotkey pressed - always use foreground window (bypass rules)
            var foregroundWindow = WindowUtils.GetForegroundWindowInfo();
            if (foregroundWindow.Handle != IntPtr.Zero)
            {
                DebugLogger.LogWindowInfo("Hotkey pressed - using foreground window (bypassing rules)", foregroundWindow);
                windowInfo = foregroundWindow;
            }
            else
            {
                DebugLogger.Log("No foreground window found");
            }
        }
        else
        {
            // Window provided (from startup/watcher) - check rules
            DebugLogger.LogWindowInfo("Using provided window (checking rules)", windowInfo);
            if (!MatchesAnyRule(windowInfo))
            {
                DebugLogger.Log("Provided window does NOT match any rule");
                windowInfo = null;
            }
            else
            {
                DebugLogger.Log("Provided window matches a rule");
            }
        }

        if (windowInfo == null || windowInfo.Handle == IntPtr.Zero)
        {
            DebugLogger.Log("No valid window to bounce - exiting");
            return;
        }

        // Normalize and resize window if needed
        DebugLogger.Log($"Normalizing window (max size: {_config.Animation.MaxWindowSizePercent}%)");
        WindowUtils.NormalizeAndResizeWindow(windowInfo.Handle, _config.Animation.MaxWindowSizePercent);
        
        // Refresh window info after normalization (unless we were given a specific window)
        if (specificWindow == null)
        {
            System.Threading.Thread.Sleep(100); // Give window time to normalize
            var refreshedWindow = WindowUtils.GetForegroundWindowInfo();
            if (refreshedWindow.Handle != IntPtr.Zero)
            {
                windowInfo = refreshedWindow;
                DebugLogger.LogWindowInfo("Refreshed window after normalization", windowInfo);
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
                DebugLogger.LogWindowInfo("Refreshed provided window after normalization", windowInfo);
            }
        }
        
        if (windowInfo.Handle == IntPtr.Zero)
        {
            DebugLogger.Log("ERROR: Window handle is zero after normalization");
            return;
        }

        _currentWindow = windowInfo;
        
        // Get bounds based on configuration
        if (_config.Animation.UseAllScreens)
        {
            _allScreensBounds = WindowUtils.GetAllScreensBounds();
            DebugLogger.Log($"Using all screens bounds: X={_allScreensBounds.X}, Y={_allScreensBounds.Y}, W={_allScreensBounds.Width}, H={_allScreensBounds.Height}");
        }
        else
        {
            _allScreensBounds = WindowUtils.GetCurrentScreenBounds(windowInfo.Handle);
            DebugLogger.Log($"Using current screen bounds: X={_allScreensBounds.X}, Y={_allScreensBounds.Y}, W={_allScreensBounds.Width}, H={_allScreensBounds.Height}");
        }

        // Initialize velocity (diagonal movement)
        _velocityX = _config.Animation.Speed;
        _velocityY = _config.Animation.Speed;
        DebugLogger.Log($"Initialized velocity: X={_velocityX}, Y={_velocityY}, Speed={_config.Animation.Speed}, Interval={_config.Animation.UpdateInterval}ms");

        _isRunning = true;
        DebugLogger.Log("Bouncing animation started");

        // Start animation timer
        _animationTimer = new System.Windows.Forms.Timer
        {
            Interval = _config.Animation.UpdateInterval
        };
        _animationTimer.Tick += AnimationTimer_Tick;
        _animationTimer.Start();
        DebugLogger.Log($"Animation timer started with interval: {_config.Animation.UpdateInterval}ms");
    }

    public void Stop()
    {
        if (!_isRunning)
        {
            DebugLogger.Log("Stop() called but bouncing is not running");
            return;
        }

        DebugLogger.Log("Stopping bouncing animation");
        _isRunning = false;
        _animationTimer?.Stop();
        _animationTimer?.Dispose();
        _animationTimer = null;
        
        if (_currentWindow != null)
        {
            DebugLogger.LogWindowInfo("Stopped bouncing window", _currentWindow);
        }
        _currentWindow = null;

        BouncingStopped?.Invoke(this, EventArgs.Empty);
        DebugLogger.Log("Bouncing animation stopped");
    }

    private int _tickCount = 0;
    private int _lastLoggedTick = 0;
    private DateTime _lastConfettiTime = DateTime.MinValue;
    private const int CONFETTI_COOLDOWN_MS = 500; // 500ms cooldown between confetti animations

    private void ShowConfetti()
    {
        // Cooldown to prevent too many confetti animations
        var timeSinceLastConfetti = (DateTime.Now - _lastConfettiTime).TotalMilliseconds;
        if (timeSinceLastConfetti < CONFETTI_COOLDOWN_MS)
        {
            DebugLogger.Log($"Confetti cooldown active: {CONFETTI_COOLDOWN_MS - (int)timeSinceLastConfetti}ms remaining");
            return;
        }
        
        try
        {
            DebugLogger.Log("PERFECT HIT! Showing confetti animation");
            _lastConfettiTime = DateTime.Now;
            var confettiForm = new ConfettiForm();
            confettiForm.Show();
        }
        catch (Exception ex)
        {
            DebugLogger.Log($"Error showing confetti: {ex.Message}");
        }
    }

    private void AnimationTimer_Tick(object? sender, EventArgs e)
    {
        if (!_isRunning || _currentWindow == null)
        {
            DebugLogger.Log("Animation timer tick but bouncing not running or window is null");
            Stop();
            return;
        }

        _tickCount++;
        
        // Calculate new position
        int newX = _currentWindow.X + _velocityX;
        int newY = _currentWindow.Y + _velocityY;

        // Check boundaries and bounce
        bool perfectHit = false;
        // Use a stricter margin for "perfect" hits - 0.5% or 2px, whichever is smaller
        int marginX = Math.Min(2, Math.Max(1, (int)(_allScreensBounds.Width * 0.005))); // 0.5% margin, max 2px
        int marginY = Math.Min(2, Math.Max(1, (int)(_allScreensBounds.Height * 0.005))); // 0.5% margin, max 2px
        
        // Store original position before clamping to check for perfect hit
        int originalX = newX;
        int originalY = newY;
        
        if (newX <= _allScreensBounds.Left)
        {
            // Check if it's a perfect hit (within 1% margin) - window is exactly at or very close to edge
            int distanceFromEdge = Math.Abs(originalX - _allScreensBounds.Left);
            perfectHit = distanceFromEdge <= marginX;
            
            newX = _allScreensBounds.Left;
            _velocityX = Math.Abs(_velocityX); // Bounce right
            DebugLogger.Log($"Bounced off left edge: X={newX}, originalX={originalX}, distance={distanceFromEdge}, margin={marginX}, perfectHit={perfectHit}");
        }
        else if (newX + _currentWindow.Width >= _allScreensBounds.Right)
        {
            // Check if it's a perfect hit (within 1% margin) - window edge is exactly at or very close to screen edge
            int rightEdge = originalX + _currentWindow.Width;
            int distanceFromEdge = Math.Abs(rightEdge - _allScreensBounds.Right);
            perfectHit = distanceFromEdge <= marginX;
            
            newX = _allScreensBounds.Right - _currentWindow.Width;
            _velocityX = -Math.Abs(_velocityX); // Bounce left
            DebugLogger.Log($"Bounced off right edge: X={newX}, rightEdge={rightEdge}, distance={distanceFromEdge}, margin={marginX}, perfectHit={perfectHit}");
        }

        if (newY <= _allScreensBounds.Top)
        {
            // Check if it's a perfect hit (within 1% margin)
            int distanceFromEdge = Math.Abs(originalY - _allScreensBounds.Top);
            perfectHit = perfectHit || distanceFromEdge <= marginY;
            
            newY = _allScreensBounds.Top;
            _velocityY = Math.Abs(_velocityY); // Bounce down
            DebugLogger.Log($"Bounced off top edge: Y={newY}, originalY={originalY}, distance={distanceFromEdge}, margin={marginY}, perfectHit={perfectHit}");
        }
        else if (newY + _currentWindow.Height >= _allScreensBounds.Bottom)
        {
            // Check if it's a perfect hit (within 1% margin)
            int bottomEdge = originalY + _currentWindow.Height;
            int distanceFromEdge = Math.Abs(bottomEdge - _allScreensBounds.Bottom);
            perfectHit = perfectHit || distanceFromEdge <= marginY;
            
            newY = _allScreensBounds.Bottom - _currentWindow.Height;
            _velocityY = -Math.Abs(_velocityY); // Bounce up
            DebugLogger.Log($"Bounced off bottom edge: Y={newY}, bottomEdge={bottomEdge}, distance={distanceFromEdge}, margin={marginY}, perfectHit={perfectHit}");
        }

        // Show confetti on perfect hit (only once per bounce, not for every edge)
        if (perfectHit)
        {
            ShowConfetti();
        }

        // Log position every 60 ticks (roughly once per second at 16ms interval)
        if (_tickCount - _lastLoggedTick >= 60)
        {
            DebugLogger.Log($"Position update: ({newX}, {newY}), Velocity: ({_velocityX}, {_velocityY}), Tick: {_tickCount}");
            _lastLoggedTick = _tickCount;
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

        // Check if there are any enabled rules at all
        bool hasEnabledRules = _config.WindowRules.Any(r => r.Enabled);
        if (!hasEnabledRules)
        {
            DebugLogger.Log("All rules are disabled - no windows match");
            return false; // All rules disabled means no match
        }

        // Check if any enabled rule matches
        int ruleIndex = 0;
        foreach (var rule in _config.WindowRules)
        {
            ruleIndex++;
            if (!rule.Enabled)
            {
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
        DebugLogger.Log("Updating WindowBouncer configuration");
        DebugLogger.Log($"New config: Speed={config.Animation.Speed}, Interval={config.Animation.UpdateInterval}ms, MaxSize={config.Animation.MaxWindowSizePercent}%, UseAllScreens={config.Animation.UseAllScreens}");
        _config = config;
        
        // Update timer interval if running
        if (_isRunning && _animationTimer != null)
        {
            _animationTimer.Interval = _config.Animation.UpdateInterval;
            DebugLogger.Log($"Updated animation timer interval to {_config.Animation.UpdateInterval}ms");
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
