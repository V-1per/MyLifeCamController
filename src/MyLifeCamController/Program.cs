using MyLifeCamController.Core;
using MyLifeCamController.Core.Settings;

namespace MyLifeCamController;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        ApplyLanguage(SettingsStore.Load(SettingsStore.DefaultPath).Language);

        if (args.Any(a => string.Equals(a, "--apply", StringComparison.OrdinalIgnoreCase)))
        {
            return ApplyMode.Run();
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
        return 0;
    }

    internal static void ApplyLanguage(string? language)
    {
        if (language == "pt" || language == "en")
        {
            Lang.IsPortuguese = language == "pt";
        }
    }
}
