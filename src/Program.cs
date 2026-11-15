using Avalonia;
using Avalonia.Controls;

namespace ClipboardHistory;

internal static class Program
{
    public static bool SetupMode { get; private set; }

    [STAThread]
    public static void Main(string[] args)
    {
        if (args.Contains("--uninstall")) { Installer.Uninstall(); return; }
        if (args.Contains("--install")) { Installer.Install(!args.Contains("--no-startup")); return; } // silent install

        SetupMode = Installer.IsSetupMode(args);
        if (SetupMode)
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args, ShutdownMode.OnLastWindowClose);
            return;
        }

        // Single instance: a second launch just exits.
        using var mutex = new Mutex(true, "ClipboardHistory.SingleInstance", out bool isNew);
        if (!isNew) return;

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args, ShutdownMode.OnExplicitShutdown);
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
