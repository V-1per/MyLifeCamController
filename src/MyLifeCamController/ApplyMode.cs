using System.Text;
using MyLifeCamController.Core.Camera;
using MyLifeCamController.Core.Settings;

namespace MyLifeCamController;

/// <summary>
/// "--apply": applies the saved settings to every connected camera without showing a window.
/// Used at Windows sign-in, when the camera may take a few seconds to appear.
/// </summary>
internal static class ApplyMode
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(3);

    public static int Run()
    {
        var log = new StringBuilder();
        log.AppendLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} --apply");

        var settings = SettingsStore.Load(SettingsStore.DefaultPath);
        var pending = settings.Devices
            .Where(d => d.Value.Properties.Count > 0)
            .Select(d => d.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var deadline = DateTime.UtcNow + Timeout;
        while (pending.Count > 0)
        {
            foreach (var device in SafeList(log))
            {
                if (!pending.Contains(device.SettingsKey))
                {
                    continue;
                }

                try
                {
                    using var session = new CameraSession(device);
                    int n = SettingsApplier.Apply(
                        SettingsApplier.For(session),
                        settings.Devices[device.SettingsKey],
                        (p, ex) => log.AppendLine($"  {device.Name}: {p.Key} falhou: {ex.Message}"));
                    log.AppendLine($"  {device.Name}: {n} definições aplicadas");
                    pending.Remove(device.SettingsKey);
                }
                catch (Exception ex)
                {
                    log.AppendLine($"  {device.Name}: erro ao abrir: {ex.Message}");
                }
            }

            if (pending.Count == 0 || DateTime.UtcNow >= deadline)
            {
                break;
            }

            Thread.Sleep(RetryDelay);
        }

        if (pending.Count > 0)
        {
            log.AppendLine($"  {pending.Count} câmara(s) guardada(s) não encontrada(s).");
        }

        WriteLog(log.ToString());
        return pending.Count == 0 ? 0 : 1;
    }

    private static IReadOnlyList<CameraDeviceInfo> SafeList(StringBuilder log)
    {
        try
        {
            return CameraDeviceInfo.ListConnected();
        }
        catch (Exception ex)
        {
            log.AppendLine($"  erro a listar câmaras: {ex.Message}");
            return [];
        }
    }

    private static void WriteLog(string text)
    {
        try
        {
            string path = Path.Combine(Path.GetDirectoryName(SettingsStore.DefaultPath)!, "apply.log");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, text);
        }
        catch (IOException)
        {
            // Logging is best effort.
        }
    }
}
