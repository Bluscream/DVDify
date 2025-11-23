using System.Runtime.InteropServices;
using System.Windows.Forms;
using DVDify.Models;

namespace DVDify;

public class HotkeyManager : IDisposable
{
    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private const int WM_HOTKEY = 0x0312;
    private IntPtr _windowHandle;
    private int _hotkeyId = 1;
    private bool _registered = false;
    private HotkeyConfig _config;

    public event EventHandler? HotkeyPressed;

    public HotkeyManager(IntPtr windowHandle, HotkeyConfig config)
    {
        _windowHandle = windowHandle;
        _config = config;
    }

    public bool Register()
    {
        if (_registered)
        {
            DebugLogger.Log("Hotkey already registered, unregistering first");
            Unregister();
        }

        uint modifiers = 0;
        var modList = new List<string>();
        if (_config.WinKey) { modifiers |= 0x0008; modList.Add("Win"); }
        if (_config.CtrlKey) { modifiers |= 0x0002; modList.Add("Ctrl"); }
        if (_config.AltKey) { modifiers |= 0x0001; modList.Add("Alt"); }
        if (_config.ShiftKey) { modifiers |= 0x0004; modList.Add("Shift"); }

        var keyName = ((Keys)_config.KeyCode).ToString();
        var hotkeyStr = string.Join("+", modList) + (modList.Count > 0 ? "+" : "") + keyName;
        DebugLogger.Log($"Registering hotkey: {hotkeyStr} (modifiers: 0x{modifiers:X}, keyCode: {_config.KeyCode})");

        _registered = RegisterHotKey(_windowHandle, _hotkeyId, modifiers, (uint)_config.KeyCode);
        
        if (_registered)
        {
            DebugLogger.Log($"Hotkey registered successfully: {hotkeyStr}");
        }
        else
        {
            DebugLogger.Log($"ERROR: Failed to register hotkey: {hotkeyStr} (may already be in use)");
        }
        
        return _registered;
    }

    public void Unregister()
    {
        if (_registered)
        {
            DebugLogger.Log("Unregistering hotkey");
            UnregisterHotKey(_windowHandle, _hotkeyId);
            _registered = false;
            DebugLogger.Log("Hotkey unregistered");
        }
    }

    public void ProcessMessage(Message m)
    {
        if (m.Msg == WM_HOTKEY && m.WParam.ToInt32() == _hotkeyId)
        {
            DebugLogger.Log("Hotkey pressed detected in ProcessMessage");
            HotkeyPressed?.Invoke(this, EventArgs.Empty);
        }
    }

    public void UpdateConfig(HotkeyConfig config)
    {
        DebugLogger.Log("Updating hotkey configuration");
        _config = config;
        if (_registered)
        {
            Register();
        }
    }

    public void Dispose()
    {
        Unregister();
    }
}
