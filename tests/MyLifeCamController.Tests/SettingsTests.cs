using MyLifeCamController.Core.Camera;
using MyLifeCamController.Core.Settings;

namespace MyLifeCamController.Tests;

public sealed class SettingsTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "mlcc-tests-" + Guid.NewGuid().ToString("N"));
    private string FilePath => Path.Combine(_dir, "settings.json");

    public void Dispose()
    {
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, recursive: true);
        }
    }

    private static CameraDeviceInfo Device(string path = @"\\?\usb#vid_045e&pid_0772") =>
        new("Microsoft LifeCam Studio", path, "@device:pnp:" + path);

    [Fact]
    public void Missing_file_gives_empty_settings()
    {
        var settings = SettingsStore.Load(FilePath);
        Assert.Empty(settings.Devices);
        Assert.Null(settings.LastDevice);
    }

    [Fact]
    public void Corrupt_file_gives_empty_settings()
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(FilePath, "{ this is not json");
        Assert.Empty(SettingsStore.Load(FilePath).Devices);
    }

    [Fact]
    public void Save_then_load_round_trips_values()
    {
        var settings = new AppSettings();
        var device = Device();
        settings.LastDevice = device.SettingsKey;
        var ds = settings.GetOrCreate(device);
        ds.Properties["Camera.Focus"] = new PropertySetting { Value = 12, Auto = false };
        ds.Properties["Image.WhiteBalance"] = new PropertySetting { Value = 4500, Auto = true };

        SettingsStore.Save(settings, FilePath);
        var loaded = SettingsStore.Load(FilePath);

        Assert.Equal(device.SettingsKey, loaded.LastDevice);
        var lds = loaded.Devices[device.SettingsKey];
        Assert.Equal("Microsoft LifeCam Studio", lds.Name);
        Assert.Equal(12, lds.Properties["Camera.Focus"].Value);
        Assert.False(lds.Properties["Camera.Focus"].Auto);
        Assert.True(lds.Properties["Image.WhiteBalance"].Auto);
        Assert.False(File.Exists(FilePath + ".tmp"));
    }

    [Fact]
    public void Language_choice_round_trips()
    {
        SettingsStore.Save(new AppSettings { Language = "en" }, FilePath);
        Assert.Equal("en", SettingsStore.Load(FilePath).Language);
    }

    [Fact]
    public void Lookups_are_case_insensitive_after_loading()
    {
        var settings = new AppSettings();
        settings.GetOrCreate(Device(@"\\?\USB#VID_045E")).Properties["Camera.Focus"] = new PropertySetting { Value = 3 };
        SettingsStore.Save(settings, FilePath);

        var loaded = SettingsStore.Load(FilePath);
        Assert.True(loaded.Devices.ContainsKey(@"\\?\usb#vid_045e"));
        Assert.True(loaded.Devices[@"\\?\usb#vid_045e"].Properties.ContainsKey("camera.focus"));
    }

    [Fact]
    public void Device_without_path_uses_moniker_as_key()
    {
        var device = new CameraDeviceInfo("Virtual Cam", "", "@device:sw:{abc}");
        Assert.Equal("@device:sw:{abc}", device.SettingsKey);
    }
}
