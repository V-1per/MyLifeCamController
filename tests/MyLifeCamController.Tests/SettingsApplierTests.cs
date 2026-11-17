using MyLifeCamController.Core.Camera;
using MyLifeCamController.Core.Settings;

namespace MyLifeCamController.Tests;

public class SettingsApplierTests
{
    private sealed class FakeTarget : SettingsApplier.ITarget
    {
        public List<CameraProperty> Props { get; } = [];
        public List<(string Key, PropertyValue Value)> Calls { get; } = [];
        public string? FailOn { get; set; }

        public IReadOnlyList<CameraProperty> Properties => Props;

        public void Set(CameraProperty property, PropertyValue value)
        {
            if (property.Key == FailOn)
            {
                throw new InvalidOperationException("driver refused");
            }

            Calls.Add((property.Key, value));
        }
    }

    private static CameraProperty Prop(string name, bool supportsAuto = true) =>
        new(PropertyCatalog.All.Single(p => p.Name == name), 0, 40, 1, 0, supportsAuto, true);

    private static DeviceSettings Saved(params (string Key, int Value, bool Auto)[] values)
    {
        var ds = new DeviceSettings();
        foreach (var (key, value, auto) in values)
        {
            ds.Properties[key] = new PropertySetting { Value = value, Auto = auto };
        }

        return ds;
    }

    [Fact]
    public void Applies_manual_values()
    {
        var target = new FakeTarget();
        target.Props.Add(Prop("Focus"));

        int applied = SettingsApplier.Apply(target, Saved(("Camera.Focus", 12, false)));

        Assert.Equal(1, applied);
        Assert.Equal([("Camera.Focus", new PropertyValue(12, false))], target.Calls);
    }

    [Fact]
    public void Auto_values_write_manual_value_first_then_enable_auto()
    {
        var target = new FakeTarget();
        target.Props.Add(Prop("Focus"));

        SettingsApplier.Apply(target, Saved(("Camera.Focus", 12, true)));

        Assert.Equal(
            [("Camera.Focus", new PropertyValue(12, false)), ("Camera.Focus", new PropertyValue(12, true))],
            target.Calls);
    }

    [Fact]
    public void Auto_is_skipped_when_camera_does_not_support_it()
    {
        var target = new FakeTarget();
        target.Props.Add(Prop("Brightness", supportsAuto: false));

        SettingsApplier.Apply(target, Saved(("Image.Brightness", 100, true)));

        Assert.Single(target.Calls);
        Assert.False(target.Calls[0].Value.Auto);
    }

    [Fact]
    public void Ignores_saved_values_for_unsupported_properties()
    {
        var target = new FakeTarget();
        target.Props.Add(Prop("Focus"));

        int applied = SettingsApplier.Apply(target, Saved(("Camera.Iris", 3, false)));

        Assert.Equal(0, applied);
        Assert.Empty(target.Calls);
    }

    [Fact]
    public void One_failing_property_does_not_stop_the_others()
    {
        var target = new FakeTarget { FailOn = "Camera.Focus" };
        target.Props.Add(Prop("Focus"));
        target.Props.Add(Prop("Brightness"));
        var errors = new List<string>();

        int applied = SettingsApplier.Apply(
            target,
            Saved(("Camera.Focus", 1, false), ("Image.Brightness", 2, false)),
            (p, _) => errors.Add(p.Key));

        Assert.Equal(1, applied);
        Assert.Equal(["Camera.Focus"], errors);
        Assert.Equal("Image.Brightness", target.Calls.Single().Key);
    }
}
