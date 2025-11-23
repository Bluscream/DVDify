using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace DVDify;

public static class WindowUtils
{
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
    
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

    [DllImport("user32.dll")]
    private static extern bool IsZoomed(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool BringWindowToTop(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("kernel32.dll")]
    private static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

    [DllImport("psapi.dll", CharSet = CharSet.Auto)]
    private static extern int GetModuleFileNameEx(IntPtr hProcess, IntPtr hModule, StringBuilder lpFilename, int nSize);

    [DllImport("kernel32.dll")]
    private static extern bool CloseHandle(IntPtr hObject);

    private const uint PROCESS_QUERY_INFORMATION = 0x0400;
    private const uint PROCESS_VM_READ = 0x0010;
    private const int SWP_NOZORDER = 0x0004;
    private const int SWP_NOSIZE = 0x0001;
    private const int SWP_DRAWFRAME = 0x0020;
    private const int SWP_SHOWWINDOW = 0x0040;
    private const int SW_RESTORE = 9;
    
    [DllImport("user32.dll")]
    private static extern bool InvalidateRect(IntPtr hWnd, IntPtr lpRect, bool bErase);
    
    [DllImport("user32.dll")]
    private static extern bool UpdateWindow(IntPtr hWnd);
    
    [DllImport("user32.dll")]
    private static extern bool RedrawWindow(IntPtr hWnd, IntPtr lprcUpdate, IntPtr hrgnUpdate, uint flags);
    
    private const uint RDW_INVALIDATE = 0x0001;
    private const uint RDW_UPDATENOW = 0x0100;
    private const uint RDW_ERASE = 0x0004;

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    public static WindowInfo GetForegroundWindowInfo()
    {
        var hWnd = GetForegroundWindow();
        if (hWnd == IntPtr.Zero)
            return new WindowInfo();

        var info = new WindowInfo
        {
            Handle = hWnd
        };

        // Get window title
        var title = new StringBuilder(256);
        GetWindowText(hWnd, title, title.Capacity);
        info.WindowName = title.ToString();

        // Get class name
        var className = new StringBuilder(256);
        GetClassName(hWnd, className, className.Capacity);
        info.ClassName = className.ToString();

        // Get executable path
        GetWindowThreadProcessId(hWnd, out uint processId);
        var hProcess = OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_VM_READ, false, processId);
        if (hProcess != IntPtr.Zero)
        {
            var exePath = new StringBuilder(1024);
            if (GetModuleFileNameEx(hProcess, IntPtr.Zero, exePath, exePath.Capacity) > 0)
            {
                info.ExecutablePath = exePath.ToString();
            }
            CloseHandle(hProcess);
        }

        // Get window position and size
        if (GetWindowRect(hWnd, out RECT rect))
        {
            info.X = rect.Left;
            info.Y = rect.Top;
            info.Width = rect.Right - rect.Left;
            info.Height = rect.Bottom - rect.Top;
        }

        return info;
    }

    public static void SetWindowPosition(IntPtr hWnd, int x, int y)
    {
        // Get current window size first
        if (!GetWindowRect(hWnd, out RECT rect))
            return;
            
        int width = rect.Right - rect.Left;
        int height = rect.Bottom - rect.Top;
        
        // Try MoveWindow first (better for some windows like Unity)
        // bRepaint = true forces immediate redraw
        if (!MoveWindow(hWnd, x, y, width, height, true))
        {
            // Fallback to SetWindowPos if MoveWindow fails
            SetWindowPos(hWnd, IntPtr.Zero, x, y, 0, 0, 
                SWP_NOSIZE | SWP_NOZORDER | SWP_DRAWFRAME | SWP_SHOWWINDOW);
        }
        
        // Only force redraw if MoveWindow didn't handle it (bRepaint=true should handle it)
        // Reduced to single redraw call for better performance
        RedrawWindow(hWnd, IntPtr.Zero, IntPtr.Zero, RDW_INVALIDATE | RDW_UPDATENOW | RDW_ERASE);
    }

    public static void NormalizeAndResizeWindow(IntPtr hWnd, int maxSizePercent)
    {
        if (hWnd == IntPtr.Zero)
        {
            DebugLogger.Log("NormalizeAndResizeWindow: Window handle is zero");
            return;
        }

        DebugLogger.Log($"Normalizing window: Handle={hWnd}, MaxSizePercent={maxSizePercent}");

        // Check if window is maximized and restore it
        if (IsZoomed(hWnd))
        {
            DebugLogger.Log("Window is maximized, restoring...");
            ShowWindow(hWnd, SW_RESTORE);
            // Give the window a moment to restore
            System.Threading.Thread.Sleep(50);
            DebugLogger.Log("Window restored from maximized state");
        }

        // Get current window rect
        if (!GetWindowRect(hWnd, out RECT rect))
        {
            DebugLogger.Log("ERROR: Failed to get window rect");
            return;
        }

        int currentWidth = rect.Right - rect.Left;
        int currentHeight = rect.Bottom - rect.Top;
        DebugLogger.Log($"Current window size: {currentWidth}x{currentHeight}");

        // Get the screen that contains the window center
        int centerX = (rect.Left + rect.Right) / 2;
        int centerY = (rect.Top + rect.Bottom) / 2;
        Screen? targetScreen = Screen.FromPoint(new System.Drawing.Point(centerX, centerY));
        if (targetScreen == null)
            targetScreen = Screen.PrimaryScreen;

        if (targetScreen == null)
            return;

        // Calculate max size based on screen dimensions
        int maxWidth = (int)(targetScreen.WorkingArea.Width * (maxSizePercent / 100.0));
        int maxHeight = (int)(targetScreen.WorkingArea.Height * (maxSizePercent / 100.0));

        // Resize if window is too large
        int newWidth = currentWidth;
        int newHeight = currentHeight;
        bool needsResize = false;

        if (currentWidth > maxWidth)
        {
            newWidth = maxWidth;
            needsResize = true;
        }

        if (currentHeight > maxHeight)
        {
            newHeight = maxHeight;
            needsResize = true;
        }

        if (needsResize)
        {
            // Keep aspect ratio if both dimensions exceed limits
            if (currentWidth > maxWidth && currentHeight > maxHeight)
            {
                double aspectRatio = (double)currentWidth / currentHeight;
                if (maxWidth / aspectRatio <= maxHeight)
                {
                    newHeight = (int)(maxWidth / aspectRatio);
                    newWidth = maxWidth;
                }
                else
                {
                    newWidth = (int)(maxHeight * aspectRatio);
                    newHeight = maxHeight;
                }
            }

            // Center the window on the screen if we're resizing
            int newX = targetScreen.WorkingArea.Left + (targetScreen.WorkingArea.Width - newWidth) / 2;
            int newY = targetScreen.WorkingArea.Top + (targetScreen.WorkingArea.Height - newHeight) / 2;

            DebugLogger.Log($"Resizing window from {currentWidth}x{currentHeight} to {newWidth}x{newHeight} at ({newX}, {newY})");
            SetWindowPos(hWnd, IntPtr.Zero, newX, newY, newWidth, newHeight, SWP_NOZORDER);
        }
        else
        {
            DebugLogger.Log("Window size is within limits, no resize needed");
        }
    }

    public static Rectangle GetAllScreensBounds()
    {
        var screens = Screen.AllScreens;
        if (screens.Length == 0)
            return Screen.PrimaryScreen?.Bounds ?? new Rectangle(0, 0, 1920, 1080);

        int minX = int.MaxValue, minY = int.MaxValue;
        int maxX = int.MinValue, maxY = int.MinValue;

        foreach (var screen in screens)
        {
            minX = Math.Min(minX, screen.Bounds.Left);
            minY = Math.Min(minY, screen.Bounds.Top);
            maxX = Math.Max(maxX, screen.Bounds.Right);
            maxY = Math.Max(maxY, screen.Bounds.Bottom);
        }

        return new Rectangle(minX, minY, maxX - minX, maxY - minY);
    }

    public static Rectangle GetCurrentScreenBounds(IntPtr hWnd)
    {
        if (hWnd == IntPtr.Zero)
            return GetAllScreensBounds();

        // Get the screen that contains the window center
        if (!GetWindowRect(hWnd, out RECT rect))
            return GetAllScreensBounds();

        int centerX = (rect.Left + rect.Right) / 2;
        int centerY = (rect.Top + rect.Bottom) / 2;
        Screen? targetScreen = Screen.FromPoint(new System.Drawing.Point(centerX, centerY));
        
        if (targetScreen == null)
            targetScreen = Screen.PrimaryScreen;

        return targetScreen?.Bounds ?? GetAllScreensBounds();
    }

    public static List<WindowInfo> GetAllWindowsInfo()
    {
        var windows = new List<WindowInfo>();
        var windowList = new List<IntPtr>();

        EnumWindowsProc enumProc = (hWnd, lParam) =>
        {
            // Only include visible windows
            if (IsWindowVisible(hWnd))
            {
                windowList.Add(hWnd);
            }
            return true;
        };

        EnumWindows(enumProc, IntPtr.Zero);

        // Pre-allocate list capacity for better performance
        windows.Capacity = windowList.Count;

        foreach (var hWnd in windowList)
        {
            var info = GetWindowInfo(hWnd);
            if (info.Handle != IntPtr.Zero && (!string.IsNullOrEmpty(info.WindowName) || !string.IsNullOrEmpty(info.ClassName)))
            {
                windows.Add(info);
            }
        }

        return windows;
    }
    
    // Optimized version that skips expensive executable path lookup for window matching
    public static List<WindowInfo> GetAllWindowsInfoFast()
    {
        var windows = new List<WindowInfo>();
        var windowList = new List<IntPtr>();

        EnumWindowsProc enumProc = (hWnd, lParam) =>
        {
            if (IsWindowVisible(hWnd))
            {
                windowList.Add(hWnd);
            }
            return true;
        };

        EnumWindows(enumProc, IntPtr.Zero);
        windows.Capacity = windowList.Count;

        foreach (var hWnd in windowList)
        {
            var info = new WindowInfo { Handle = hWnd };
            
            // Get window title
            var title = new StringBuilder(256);
            GetWindowText(hWnd, title, title.Capacity);
            info.WindowName = title.ToString();

            // Get class name
            var className = new StringBuilder(256);
            GetClassName(hWnd, className, className.Capacity);
            info.ClassName = className.ToString();

            // Get window position and size (skip expensive executable path lookup)
            if (GetWindowRect(hWnd, out RECT rect))
            {
                info.X = rect.Left;
                info.Y = rect.Top;
                info.Width = rect.Right - rect.Left;
                info.Height = rect.Bottom - rect.Top;
            }

            if (info.Handle != IntPtr.Zero && (!string.IsNullOrEmpty(info.WindowName) || !string.IsNullOrEmpty(info.ClassName)))
            {
                windows.Add(info);
            }
        }

        return windows;
    }

    public static WindowInfo GetWindowInfo(IntPtr hWnd)
    {
        if (hWnd == IntPtr.Zero)
            return new WindowInfo();

        var info = new WindowInfo
        {
            Handle = hWnd
        };

        // Get window title
        var title = new StringBuilder(256);
        GetWindowText(hWnd, title, title.Capacity);
        info.WindowName = title.ToString();

        // Get class name
        var className = new StringBuilder(256);
        GetClassName(hWnd, className, className.Capacity);
        info.ClassName = className.ToString();

        // Get executable path
        GetWindowThreadProcessId(hWnd, out uint processId);
        var hProcess = OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_VM_READ, false, processId);
        if (hProcess != IntPtr.Zero)
        {
            var exePath = new StringBuilder(1024);
            if (GetModuleFileNameEx(hProcess, IntPtr.Zero, exePath, exePath.Capacity) > 0)
            {
                info.ExecutablePath = exePath.ToString();
            }
            CloseHandle(hProcess);
        }

        // Get window position and size
        if (GetWindowRect(hWnd, out RECT rect))
        {
            info.X = rect.Left;
            info.Y = rect.Top;
            info.Width = rect.Right - rect.Left;
            info.Height = rect.Bottom - rect.Top;
        }

        return info;
    }

    public static string DumpAllWindowsToFile()
    {
        var windows = GetAllWindowsInfo();
        
        // Save to dumps folder in AppData
        var dumpsFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "DVDify",
            "dumps"
        );
        
        if (!Directory.Exists(dumpsFolder))
        {
            Directory.CreateDirectory(dumpsFolder);
        }
        
        var dumpFile = Path.Combine(dumpsFolder, $"DVDify_Windows_Dump_{DateTime.Now:yyyyMMdd_HHmmss}.txt");

        using (var writer = new StreamWriter(dumpFile, false, Encoding.UTF8))
        {
            writer.WriteLine("DVDify - All Active Windows Dump");
            writer.WriteLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            writer.WriteLine(new string('=', 80));
            writer.WriteLine();

            foreach (var window in windows)
            {
                writer.WriteLine($"Window Title: {window.WindowName}");
                writer.WriteLine($"Class Name: {window.ClassName}");
                writer.WriteLine($"Executable: {window.ExecutablePath}");
                writer.WriteLine($"Handle: {window.Handle}");
                writer.WriteLine($"Position: ({window.X}, {window.Y})");
                writer.WriteLine($"Size: {window.Width} x {window.Height}");
                writer.WriteLine();
                writer.WriteLine("AutoHotkey format:");
                writer.WriteLine($"ahk_class {window.ClassName}");
                if (!string.IsNullOrEmpty(window.ExecutablePath))
                {
                    var exeName = Path.GetFileName(window.ExecutablePath);
                    writer.WriteLine($"ahk_exe {exeName}");
                }
                writer.WriteLine(new string('-', 80));
                writer.WriteLine();
            }
        }

        return dumpFile;
    }

    public static void BringWindowToForeground(IntPtr hWnd)
    {
        if (hWnd == IntPtr.Zero)
            return;

        BringWindowToTop(hWnd);
        SetForegroundWindow(hWnd);
    }
}

public class WindowInfo
{
    public IntPtr Handle { get; set; }
    public string WindowName { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public string ExecutablePath { get; set; } = string.Empty;
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
}
