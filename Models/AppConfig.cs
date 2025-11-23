using System.Collections.Generic;

namespace DVDify.Models;

public class AppConfig
{
    public HotkeyConfig Hotkey { get; set; } = new();
    public AnimationConfig Animation { get; set; } = new();
    public ConfettiConfig Confetti { get; set; } = new();
    public List<WindowRule> WindowRules { get; set; } = new();
    public bool DebugLogging { get; set; } = false;
}

public class HotkeyConfig
{
    public bool WinKey { get; set; } = true;
    public bool CtrlKey { get; set; } = false;
    public bool AltKey { get; set; } = false;
    public bool ShiftKey { get; set; } = false;
    public int KeyCode { get; set; } = 68; // D key
}

public class AnimationConfig
{
    public int Speed { get; set; } = 5; // pixels per frame
    public int UpdateInterval { get; set; } = 16; // milliseconds (60 FPS)
    public int MaxWindowSizePercent { get; set; } = 50; // Maximum window size as percentage of screen (1-100)
    public bool UseAllScreens { get; set; } = true; // If true, bounce across all screens; if false, only current screen
}

public class ConfettiConfig
{
    public bool Enabled { get; set; } = true;
    public int ParticleCount { get; set; } = 150;
    public int DurationFrames { get; set; } = 60; // Animation duration in frames (~1 second at 60fps)
    public double PerfectHitMarginPercent { get; set; } = 0.5; // Percentage of screen for perfect hit detection
}
