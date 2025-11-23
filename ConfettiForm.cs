using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace DVDify;

public class ConfettiForm : Form
{
    [DllImport("user32.dll", SetLastError = true)]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
    
    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
    
    [DllImport("user32.dll")]
    private static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);
    
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_LAYERED = 0x80000;
    private const int WS_EX_TRANSPARENT = 0x20;
    private const uint LWA_ALPHA = 0x2;
    private const uint LWA_COLORKEY = 0x1;
    
    private readonly List<ConfettiParticle> _particles = new();
    private readonly System.Windows.Forms.Timer _animationTimer;
    private readonly Random _random = new();
    private int _frameCount = 0;
    private const int MAX_FRAMES = 60; // ~1 second at 60fps

    public ConfettiForm()
    {
        FormBorderStyle = FormBorderStyle.None;
        WindowState = FormWindowState.Normal; // Don't maximize, set size manually
        TopMost = true;
        ShowInTaskbar = false;
        BackColor = Color.Black; // Use black for transparency key
        TransparencyKey = Color.Black; // Black pixels become transparent
        StartPosition = FormStartPosition.Manual;
        Opacity = 1.0; // Fully opaque (transparency handled via TransparencyKey)
        
        // Cover all screens
        var allScreens = Screen.AllScreens;
        int minX = allScreens.Min(s => s.Bounds.Left);
        int minY = allScreens.Min(s => s.Bounds.Top);
        int maxX = allScreens.Max(s => s.Bounds.Right);
        int maxY = allScreens.Max(s => s.Bounds.Bottom);
        
        Location = new Point(minX, minY);
        Size = new Size(maxX - minX, maxY - minY);
        
        // Make it click-through and transparent
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.DoubleBuffer, true);
        
        // Make window click-through and transparent using Win32 API
        // Set up in CreateParams to avoid white flash
        
        // Create particles - spread across all screens
        for (int i = 0; i < 150; i++)
        {
            _particles.Add(new ConfettiParticle
            {
                X = _random.Next(Width),
                Y = _random.Next(Height / 3), // Start in upper third
                VelocityX = (float)((_random.NextDouble() - 0.5) * 12),
                VelocityY = (float)(_random.NextDouble() * 6 + 3),
                Color = GetRandomColor(),
                Size = _random.Next(6, 18),
                Rotation = _random.Next(360),
                RotationSpeed = (float)((_random.NextDouble() - 0.5) * 15)
            });
        }

        _animationTimer = new System.Windows.Forms.Timer { Interval = 16 }; // ~60fps
        _animationTimer.Tick += AnimationTimer_Tick;
        
        // Set up transparency before showing to prevent white flash
        Shown += (s, e) =>
        {
            if (Handle != IntPtr.Zero)
            {
                int exStyle = GetWindowLong(Handle, GWL_EXSTYLE);
                SetWindowLong(Handle, GWL_EXSTYLE, exStyle | WS_EX_LAYERED | WS_EX_TRANSPARENT);
            }
            _animationTimer.Start();
        };
    }

    private Color GetRandomColor()
    {
        var colors = new[]
        {
            Color.Red, Color.Blue, Color.Green, Color.Yellow, Color.Magenta,
            Color.Cyan, Color.Orange, Color.Pink, Color.Purple, Color.Lime
        };
        return colors[_random.Next(colors.Length)];
    }

    private void AnimationTimer_Tick(object? sender, EventArgs e)
    {
        _frameCount++;
        
        // Update particles
        foreach (var particle in _particles)
        {
            particle.X += particle.VelocityX;
            particle.Y += particle.VelocityY;
            particle.Rotation += particle.RotationSpeed;
            particle.VelocityY += 0.3f; // Gravity
        }

        Invalidate();

        if (_frameCount >= MAX_FRAMES)
        {
            _animationTimer.Stop();
            Close();
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;
        g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;

        // Clear with black (transparency key) to make background transparent
        g.Clear(Color.Black);

        // Draw particles
        foreach (var particle in _particles)
        {
            using (var brush = new SolidBrush(particle.Color))
            {
                g.TranslateTransform(particle.X, particle.Y);
                g.RotateTransform(particle.Rotation);
                g.FillRectangle(brush, -particle.Size / 2, -particle.Size / 2, particle.Size, particle.Size);
                g.ResetTransform();
            }
        }
    }
    
    protected override CreateParams CreateParams
    {
        get
        {
            CreateParams cp = base.CreateParams;
            cp.ExStyle |= 0x80000; // WS_EX_LAYERED
            cp.ExStyle |= 0x20; // WS_EX_TRANSPARENT (click-through)
            return cp;
        }
    }
    
    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        // Ensure transparency is set up
        if (Handle != IntPtr.Zero)
        {
            int exStyle = GetWindowLong(Handle, GWL_EXSTYLE);
            SetWindowLong(Handle, GWL_EXSTYLE, exStyle | WS_EX_LAYERED | WS_EX_TRANSPARENT);
        }
    }
    
    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        // Ensure window is properly set up after showing
        if (Handle != IntPtr.Zero)
        {
            int exStyle = GetWindowLong(Handle, GWL_EXSTYLE);
            SetWindowLong(Handle, GWL_EXSTYLE, exStyle | WS_EX_LAYERED | WS_EX_TRANSPARENT);
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _animationTimer?.Stop();
        _animationTimer?.Dispose();
        base.OnFormClosing(e);
    }

    private class ConfettiParticle
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float VelocityX { get; set; }
        public float VelocityY { get; set; }
        public Color Color { get; set; }
        public int Size { get; set; }
        public float Rotation { get; set; }
        public float RotationSpeed { get; set; }
    }
}
