namespace MyLifeCamController.Core.Camera;

/// <summary>Which DirectShow interface a property belongs to.</summary>
public enum PropertyGroup
{
    /// <summary>IAMCameraControl: focus, exposure, zoom, pan, tilt...</summary>
    Camera,

    /// <summary>IAMVideoProcAmp: brightness, contrast, white balance...</summary>
    Image,
}

/// <summary>Static description of a property the app knows how to show.</summary>
public sealed record PropertyDefinition(PropertyGroup Group, int Id, string Name, string PortugueseName, string EnglishName)
{
    /// <summary>Name shown in the UI, in the current language.</summary>
    public string DisplayName => Lang.T(PortugueseName, EnglishName);

    /// <summary>Stable key used in the settings file, e.g. "Camera.Focus".</summary>
    public string Key => $"{Group}.{Name}";
}

/// <summary>A property the connected camera actually supports, with its range.</summary>
public sealed record CameraProperty(
    PropertyDefinition Definition,
    int Min,
    int Max,
    int Step,
    int Default,
    bool SupportsAuto,
    bool SupportsManual)
{
    public string Key => Definition.Key;
}

/// <summary>Current value of a property as reported by the camera.</summary>
public readonly record struct PropertyValue(int Value, bool Auto);

/// <summary>All properties defined by DirectShow, in the order they are shown in the UI.</summary>
public static class PropertyCatalog
{
    // IDs from the CameraControlProperty / VideoProcAmpProperty enums in strmif.h.
    public static readonly IReadOnlyList<PropertyDefinition> All =
    [
        new(PropertyGroup.Camera, 6, "Focus", "Foco", "Focus"),
        new(PropertyGroup.Camera, 4, "Exposure", "Exposição", "Exposure"),
        new(PropertyGroup.Camera, 3, "Zoom", "Zoom", "Zoom"),
        new(PropertyGroup.Camera, 0, "Pan", "Pan (horizontal)", "Pan (horizontal)"),
        new(PropertyGroup.Camera, 1, "Tilt", "Tilt (vertical)", "Tilt (vertical)"),
        new(PropertyGroup.Camera, 2, "Roll", "Rotação", "Roll"),
        new(PropertyGroup.Camera, 5, "Iris", "Íris", "Iris"),

        new(PropertyGroup.Image, 0, "Brightness", "Brilho", "Brightness"),
        new(PropertyGroup.Image, 1, "Contrast", "Contraste", "Contrast"),
        new(PropertyGroup.Image, 3, "Saturation", "Saturação", "Saturation"),
        new(PropertyGroup.Image, 4, "Sharpness", "Nitidez", "Sharpness"),
        new(PropertyGroup.Image, 7, "WhiteBalance", "Balanço de brancos", "White balance"),
        new(PropertyGroup.Image, 9, "Gain", "Ganho", "Gain"),
        new(PropertyGroup.Image, 5, "Gamma", "Gama", "Gamma"),
        new(PropertyGroup.Image, 2, "Hue", "Matiz", "Hue"),
        new(PropertyGroup.Image, 8, "BacklightCompensation", "Compensação de contraluz", "Backlight compensation"),
        new(PropertyGroup.Image, 6, "ColorEnable", "Cor", "Color"),
    ];
}
