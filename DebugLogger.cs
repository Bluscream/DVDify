using System.Text;
using System.Windows.Forms;

namespace DVDify;

public static class DebugLogger
{
    private static readonly string LogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "DVDify",
        "debug.log"
    );

    private static bool _enabled = false;
    private static readonly object _lock = new object();

    public static void Enable()
    {
        _enabled = true;
        Log("=== Debug logging enabled ===");
    }

    public static void Disable()
    {
        if (_enabled)
        {
            Log("=== Debug logging disabled ===");
            FlushLogBuffer(); // Ensure final messages are written
        }
        _enabled = false;
        _flushTimer.Enabled = false;
    }

    private static readonly StringBuilder _logBuffer = new StringBuilder();
    private static readonly System.Windows.Forms.Timer _flushTimer = new System.Windows.Forms.Timer { Interval = 1000 }; // Flush every second
    private static bool _flushTimerInitialized = false;

    static DebugLogger()
    {
        _flushTimer.Tick += (s, e) => FlushLogBuffer();
    }

    public static void Log(string message)
    {
        if (!_enabled)
            return;

        lock (_lock)
        {
            try
            {
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                _logBuffer.Append($"[{timestamp}] {message}{Environment.NewLine}");
                
                // Initialize flush timer on first log
                if (!_flushTimerInitialized)
                {
                    _flushTimer.Enabled = true;
                    _flushTimerInitialized = true;
                }
                
                // Flush immediately if buffer gets too large (>64KB)
                if (_logBuffer.Length > 65536)
                {
                    FlushLogBuffer();
                }
            }
            catch
            {
                // Silently fail if logging fails
            }
        }
    }
    
    private static void FlushLogBuffer()
    {
        if (_logBuffer.Length == 0)
            return;
            
        lock (_lock)
        {
            try
            {
                var directory = Path.GetDirectoryName(LogPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.AppendAllText(LogPath, _logBuffer.ToString(), Encoding.UTF8);
                _logBuffer.Clear();
            }
            catch
            {
                // Silently fail if logging fails
            }
        }
    }

    public static void LogWindowInfo(string context, WindowInfo window)
    {
        if (!_enabled)
            return;

        Log($"{context}: Handle={window.Handle}, Title='{window.WindowName}', Class='{window.ClassName}', Exe='{Path.GetFileName(window.ExecutablePath)}'");
    }
}
