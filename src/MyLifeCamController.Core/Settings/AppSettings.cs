using System.Text.Json;
using MyLifeCamController.Core.Camera;

namespace MyLifeCamController.Core.Settings;

/// <summary>Everything the app remembers, stored as JSON in %APPDATA%\MyLifeCamController.</summary>
public sealed class AppSettings
{
    /// <summary>Saved values per camera, keyed by <see cref="CameraDeviceInfo.SettingsKey"/>.</summary>
    public Dictionary<string, DeviceSettings> Devices { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Settings key of the camera selected last time.</summary>
    public string? LastDevice { get; set; }

    /// <summary>"pt" or "en"; null follows the Windows language.</summary>
    public string? Language { get; set; }

    public DeviceSettings GetOrCreate(CameraDeviceInfo device)
    {
        if (!Devices.TryGetValue(device.SettingsKey, out var ds))
        {
            ds = new DeviceSettings();
            Devices[device.SettingsKey] = ds;
        }

        ds.Name = device.Name;
        return ds;
    }
}

public sealed class DeviceSettings
{
    public string Name { get; set; } = string.Empty;

    /// <summary>Saved values keyed by <see cref="PropertyDefinition.Key"/>, e.g. "Camera.Focus".</summary>
    public Dictionary<string, PropertySetting> Properties { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class PropertySetting
{
    public int Value { get; set; }
    public bool Auto { get; set; }
}

public static class SettingsStore
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public static string DefaultPath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "MyLifeCamController", "settings.json");

    /// <summary>Loads settings; a missing or corrupt file gives empty settings instead of an error.</summary>
    public static AppSettings Load(string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                return new AppSettings();
            }

            var loaded = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(path), Options);
            if (loaded == null)
            {
                return new AppSettings();
            }

            // Re-create dictionaries so lookups stay case-insensitive after deserialization.
            var settings = new AppSettings { LastDevice = loaded.LastDevice, Language = loaded.Language };
            foreach (var (key, device) in loaded.Devices ?? [])
            {
                settings.Devices[key] = new DeviceSettings
                {
                    Name = device.Name,
                    Properties = new Dictionary<string, PropertySetting>(device.Properties ?? [], StringComparer.OrdinalIgnoreCase),
                };
            }

            return settings;
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            return new AppSettings();
        }
    }

    /// <summary>Saves atomically (write to temp file, then replace) so a crash never leaves half a file.</summary>
    public static void Save(AppSettings settings, string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        string temp = path + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(settings, Options));
        File.Move(temp, path, overwrite: true);
    }
}
