# DVDify

A Windows system tray application that makes windows bounce around your screen like the classic DVD screensaver when you press a hotkey.

## Features

- **System Tray Integration**: Runs quietly in the background with a system tray icon
- **Global Hotkey**: Press Win+D (or your configured hotkey) to start/stop the bouncing animation
- **Multi-Monitor Support**: Windows bounce across all connected displays
- **Window Rules**: Configure which windows should bounce based on:
  - Window name (supports glob patterns like `*Chrome*`)
  - Class name (supports glob patterns)
  - Executable path (supports glob patterns)
- **Configurable Animation**: Adjust speed and update interval
- **Case-Insensitive Matching**: All rule matching is case-insensitive

## Usage

1. Run the application - it will appear in your system tray
2. Right-click the tray icon to access:
   - **Settings**: Configure hotkey, animation settings, and window rules
   - **Exit**: Close the application
3. Press the configured hotkey (default: Win+D) to start bouncing the foreground window
4. Press the hotkey again to stop the animation

## Configuration

### Hotkey Configuration

- Click "Set Hotkey" in the settings window
- Press your desired key combination
- The hotkey will be saved automatically

### Window Rules

Add rules to match windows that should bounce:

- **Window Name**: Match against the window title (e.g., `*Notepad*`)
- **Class Name**: Match against the window class name
- **Executable Path**: Match against the full path to the executable (e.g., `*chrome.exe`)

**Glob Patterns Supported:**
- `*` - Matches any sequence of characters
- `?` - Matches any single character

**Examples:**
- `*Chrome*` - Matches any window with "Chrome" in the name
- `notepad.exe` - Matches Notepad windows
- `*Visual Studio*` - Matches Visual Studio windows

**Note**: If no rules are configured, all windows will bounce. If rules are configured, only windows matching at least one enabled rule will bounce.

### Animation Settings

- **Animation Speed**: Pixels moved per frame (1-50)
- **Update Interval**: Milliseconds between updates (1-100, lower = smoother but more CPU usage)

## Configuration File

Settings are stored in: `%AppData%\DVDify\config.json`

## Building

```bash
dotnet build
```

## Requirements

- .NET 8.0 or later
- Windows operating system
