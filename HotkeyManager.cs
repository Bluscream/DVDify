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
            Unregister();

        uint modifiers = 0;
        if (_config.WinKey) modifiers |= 0x0008; // MOD_WIN
        if (_config.CtrlKey) modifiers |= 0x0002; // MOD_CONTROL
        if (_config.AltKey) modifiers |= 0x0001; // MOD_ALT
        if (_config.ShiftKey) modifiers |= 0x0004; // MOD_SHIFT

        _registered = RegisterHotKey(_windowHandle, _hotkeyId, modifiers, (uint)_config.KeyCode);
        return _registered;
    }

    public void Unregister()
    {
        if (_registered)
        {
            UnregisterHotKey(_windowHandle, _hotkeyId);
            _registered = false;
        }
    }

    public void ProcessMessage(Message m)
    {
        if (m.Msg == WM_HOTKEY && m.WParam.ToInt32() == _hotkeyId)
        {
            HotkeyPressed?.Invoke(this, EventArgs.Empty);
        }
    }

    public void UpdateConfig(HotkeyConfig config)
    {
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
