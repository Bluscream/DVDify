using System.Windows.Forms;

namespace DVDify;

public class TestWindow : Form
{
    private static int _windowCounter = 0;
    private int _windowNumber;

    public TestWindow()
    {
        _windowNumber = ++_windowCounter;
        Text = $"DVDify Test Window #{_windowNumber}";
        Size = new Size(400, 300);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.Sizable;
        
        DebugLogger.Log($"Test window #{_windowNumber} created: '{Text}'");
        
        // Make it easy to identify
        var label = new Label
        {
            Text = $"This is a test window for DVDify.\n\nWindow #{_windowNumber}\n\nUse this window to test the bouncing animation.\n\nYou can configure rules to match this window by:\n- Window Name: \"DVDify Test Window\"\n- Class Name: \"WindowsForms10.Window.8.app.0.*\"\n- Executable: \"DVDify.exe\"",
            Dock = DockStyle.Fill,
            TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
            Padding = new Padding(20)
        };
        
        var closeButton = new Button
        {
            Text = "Close",
            Dock = DockStyle.Bottom,
            Height = 40
        };
        closeButton.Click += (s, e) => Close();
        
        Controls.Add(label);
        Controls.Add(closeButton);
        
        // Make sure it's visible
        Show();
        BringToFront();
        Activate();
    }
}
