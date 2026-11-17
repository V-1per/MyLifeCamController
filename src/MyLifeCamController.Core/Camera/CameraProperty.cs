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
public sealed record PropertyDefinition(PropertyGroup Group, int Id, string Name, string DisplayName)
{
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
        new(PropertyGroup.Camera, 6, "Focus", "Foco"),
        new(PropertyGroup.Camera, 4, "Exposure", "Exposição"),
        new(PropertyGroup.Camera, 3, "Zoom", "Zoom"),
        new(PropertyGroup.Camera, 0, "Pan", "Pan (horizontal)"),
        new(PropertyGroup.Camera, 1, "Tilt", "Tilt (vertical)"),
        new(PropertyGroup.Camera, 2, "Roll", "Rotação"),
        new(PropertyGroup.Camera, 5, "Iris", "Íris"),

        new(PropertyGroup.Image, 0, "Brightness", "Brilho"),
        new(PropertyGroup.Image, 1, "Contrast", "Contraste"),
        new(PropertyGroup.Image, 3, "Saturation", "Saturação"),
        new(PropertyGroup.Image, 4, "Sharpness", "Nitidez"),
        new(PropertyGroup.Image, 7, "WhiteBalance", "Balanço de brancos"),
        new(PropertyGroup.Image, 9, "Gain", "Ganho"),
        new(PropertyGroup.Image, 5, "Gamma", "Gama"),
        new(PropertyGroup.Image, 2, "Hue", "Matiz"),
        new(PropertyGroup.Image, 8, "BacklightCompensation", "Compensação de contraluz"),
        new(PropertyGroup.Image, 6, "ColorEnable", "Cor"),
    ];
}
