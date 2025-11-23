namespace DVDify.Models;

public class WindowRule
{
    public string? WindowName { get; set; }
    public string? ClassName { get; set; }
    public string? ExecutablePath { get; set; }
    public bool Enabled { get; set; } = true;
}
