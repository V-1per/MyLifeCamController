using MyLifeCamController.Core.Camera;
using Xunit.Abstractions;

namespace MyLifeCamController.Tests;

/// <summary>
/// Talks to the real camera. Each test returns early (passes) when no camera is connected,
/// so the suite still runs on machines without one. Run only these with:
///   dotnet test --filter Category=Hardware
/// </summary>
[Trait("Category", "Hardware")]
public class HardwareTests(ITestOutputHelper output)
{
    // Prefer a physical camera (virtual cams like OBS have no device path).
    private static CameraDeviceInfo? FirstCamera() =>
        CameraDeviceInfo.ListConnected().OrderBy(d => string.IsNullOrEmpty(d.DevicePath)).FirstOrDefault();

    [Fact]
    public void Lists_connected_cameras_without_throwing()
    {
        var devices = CameraDeviceInfo.ListConnected();
        foreach (var d in devices)
        {
            output.WriteLine($"{d.Name} | {d.DevicePath}");
            Assert.False(string.IsNullOrWhiteSpace(d.Name));
            Assert.False(string.IsNullOrWhiteSpace(d.MonikerName));
        }
    }

    [Fact]
    public void Reads_supported_properties_within_their_range()
    {
        var device = FirstCamera();
        if (device == null) return;

        using var session = new CameraSession(device);
        Assert.NotEmpty(session.Properties);

        foreach (var p in session.Properties)
        {
            var v = session.Get(p);
            output.WriteLine($"{p.Key,-28} {p.Min,6}..{p.Max,-6} step {p.Step,-3} default {p.Default,-6} " +
                             $"auto:{p.SupportsAuto,-5} now {v.Value} ({(v.Auto ? "auto" : "manual")})");
            Assert.InRange(v.Value, Math.Min(p.Min, p.Max), Math.Max(p.Min, p.Max));
        }
    }

    [Fact]
    public void Set_brightness_then_restore_original()
    {
        var device = FirstCamera();
        if (device == null) return;

        using var session = new CameraSession(device);
        var brightness = session.Properties.FirstOrDefault(p => p.Definition.Name == "Brightness");
        if (brightness == null) return;

        var original = session.Get(brightness);
        try
        {
            // Move ~10% of the range away from the current value, staying inside the range.
            int delta = Math.Max(brightness.Step, (brightness.Max - brightness.Min) / 10);
            int target = original.Value + delta <= brightness.Max ? original.Value + delta : original.Value - delta;
            session.Set(brightness, new PropertyValue(target, false));

            // The LifeCam quantizes some values internally (e.g. 30 -> 31), so allow a small tolerance.
            Assert.InRange(session.Get(brightness).Value, target - 2, target + 2);
        }
        finally
        {
            session.Set(brightness, original);
        }

        Assert.InRange(session.Get(brightness).Value, original.Value - 2, original.Value + 2);
    }
}
