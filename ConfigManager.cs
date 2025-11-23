using System.Text.Json;
using DVDify.Models;

namespace DVDify;

public static class ConfigManager
{
    private static readonly string ConfigPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "DVDify",
        "config.json"
    );

    public static AppConfig Load()
    {
        DebugLogger.Log($"Loading config from: {ConfigPath}");
        try
        {
            if (File.Exists(ConfigPath))
            {
                var json = File.ReadAllText(ConfigPath);
                var config = JsonSerializer.Deserialize<AppConfig>(json);
                if (config != null)
                {
                    DebugLogger.Log($"Config loaded successfully. Rules: {config.WindowRules?.Count ?? 0}, DebugLogging: {config.DebugLogging}");
                    return config;
                }
                else
                {
                    DebugLogger.Log("Config file exists but deserialization returned null, using defaults");
                }
            }
            else
            {
                DebugLogger.Log("Config file not found, using defaults");
            }
        }
        catch (Exception ex)
        {
            DebugLogger.Log($"ERROR loading config: {ex.GetType().Name}: {ex.Message}");
            DebugLogger.Log($"Stack trace: {ex.StackTrace}");
        }

        DebugLogger.Log("Returning default config");
        return new AppConfig();
    }

    public static void Save(AppConfig config)
    {
        DebugLogger.Log($"Saving config to: {ConfigPath}");
        try
        {
            var directory = Path.GetDirectoryName(ConfigPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                DebugLogger.Log($"Created config directory: {directory}");
            }

            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            var json = JsonSerializer.Serialize(config, options);
            File.WriteAllText(ConfigPath, json);
            DebugLogger.Log($"Config saved successfully. Rules: {config.WindowRules?.Count ?? 0}, DebugLogging: {config.DebugLogging}");
        }
        catch (Exception ex)
        {
            DebugLogger.Log($"ERROR saving config: {ex.GetType().Name}: {ex.Message}");
            DebugLogger.Log($"Stack trace: {ex.StackTrace}");
        }
    }
}
