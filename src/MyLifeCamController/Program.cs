namespace MyLifeCamController;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        if (args.Any(a => string.Equals(a, "--apply", StringComparison.OrdinalIgnoreCase)))
        {
            return ApplyMode.Run();
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
        return 0;
    }
}
