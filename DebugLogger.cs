using System.Text;

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
        }
        _enabled = false;
    }

    public static void Log(string message)
    {
        if (!_enabled)
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

                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                var logEntry = $"[{timestamp}] {message}{Environment.NewLine}";
                File.AppendAllText(LogPath, logEntry, Encoding.UTF8);
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
