using MyLifeCamController.Core.Camera;

namespace MyLifeCamController.Tests;

public class PropertyMathTests
{
    [Theory]
    [InlineData(5, 0, 10, 1, 5)]
    [InlineData(-3, 0, 10, 1, 0)]      // below min -> min
    [InlineData(42, 0, 10, 1, 10)]     // above max -> max
    [InlineData(7, 0, 40, 5, 5)]       // nearest step
    [InlineData(8, 0, 40, 5, 10)]
    [InlineData(2500, 2500, 10000, 10, 2500)]
    [InlineData(2504, 2500, 10000, 10, 2500)]
    [InlineData(2505, 2500, 10000, 10, 2510)] // midpoint rounds away from zero
    [InlineData(-6, -11, 1, 1, -6)]    // negative ranges (exposure)
    [InlineData(9, 0, 10, 4, 8)]       // max not on a step boundary: never exceed max
    [InlineData(10, 0, 10, 4, 8)]
    public void Snap_clamps_and_rounds_to_step(int value, int min, int max, int step, int expected)
    {
        Assert.Equal(expected, PropertyMath.Snap(value, min, max, step));
    }

    [Fact]
    public void Snap_handles_zero_or_negative_step_as_one()
    {
        Assert.Equal(7, PropertyMath.Snap(7, 0, 10, 0));
        Assert.Equal(7, PropertyMath.Snap(7, 0, 10, -2));
    }

    [Theory]
    [InlineData(-5, "1/32 s")]
    [InlineData(-1, "1/2 s")]
    [InlineData(0, "1 s")]
    [InlineData(2, "4 s")]
    public void Exposure_is_shown_as_seconds(int log2Seconds, string expected)
    {
        Assert.Equal(expected, PropertyMath.FormatExposure(log2Seconds));
    }

    [Fact]
    public void Format_uses_units_per_property()
    {
        var wb = PropertyCatalog.All.Single(p => p.Name == "WhiteBalance");
        var brightness = PropertyCatalog.All.Single(p => p.Name == "Brightness");
        var color = PropertyCatalog.All.Single(p => p.Name == "ColorEnable");

        Assert.Equal("4500 K", PropertyMath.Format(wb, 4500));
        Assert.Equal("133", PropertyMath.Format(brightness, 133));
        Assert.Equal("Desligada", PropertyMath.Format(color, 0));
    }

    [Fact]
    public void Catalog_keys_are_unique_and_focus_comes_first()
    {
        var keys = PropertyCatalog.All.Select(p => p.Key).ToList();
        Assert.Equal(keys.Count, keys.Distinct().Count());
        Assert.Equal("Camera.Focus", keys[0]);
    }
}
