using MyLifeCamController.Core.Camera;

namespace MyLifeCamController.Core.Settings;

/// <summary>Writes saved values back to a camera (used at startup with --apply and when opening a camera).</summary>
public static class SettingsApplier
{
    public interface ITarget
    {
        IReadOnlyList<CameraProperty> Properties { get; }
        void Set(CameraProperty property, PropertyValue value);
    }

    /// <summary>Applies every saved value the camera supports. Returns how many were applied.</summary>
    public static int Apply(ITarget target, DeviceSettings saved, Action<CameraProperty, Exception>? onError = null)
    {
        int applied = 0;
        foreach (var property in target.Properties)
        {
            if (!saved.Properties.TryGetValue(property.Key, out var setting))
            {
                continue;
            }

            try
            {
                // Write the manual value first, then switch to auto if needed: some drivers
                // ignore the value when the auto flag is set in the same call.
                target.Set(property, new PropertyValue(setting.Value, false));
                if (setting.Auto && property.SupportsAuto)
                {
                    target.Set(property, new PropertyValue(setting.Value, true));
                }

                applied++;
            }
            catch (Exception ex)
            {
                onError?.Invoke(property, ex);
            }
        }

        return applied;
    }

    /// <summary>Adapter so a real <see cref="CameraSession"/> can be used as a target.</summary>
    public static ITarget For(CameraSession session) => new SessionTarget(session);

    private sealed class SessionTarget(CameraSession session) : ITarget
    {
        public IReadOnlyList<CameraProperty> Properties => session.Properties;
        public void Set(CameraProperty property, PropertyValue value) => session.Set(property, value);
    }
}
