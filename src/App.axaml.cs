using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace ClipboardHistory;

public partial class App : Application
{
    private MainWindow? _window;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var data = Store.Load();
            Themes.Apply(data.Settings);

            if (Program.SetupMode)
            {
                desktop.MainWindow = new SetupWindow();
                base.OnFrameworkInitializationCompleted();
                return;
            }

            _window = new MainWindow(data);

            var menu = new NativeMenu();
            var open = new NativeMenuItem("Open Clipboard History   Ctrl+Shift+V");
            open.Click += (_, _) => _window.ShowPopup();
            var quit = new NativeMenuItem("Quit");
            quit.Click += (_, _) => { _window.SaveNow(); desktop.Shutdown(); };
            menu.Items.Add(open);
            menu.Items.Add(new NativeMenuItemSeparator());
            menu.Items.Add(quit);

            var tray = new TrayIcon
            {
                Icon = CreateIcon(),
                ToolTipText = "Clipboard History",
                Menu = menu,
                IsVisible = true,
            };
            tray.Clicked += (_, _) => _window.Toggle();
            TrayIcon.SetIcons(this, new TrayIcons { tray });
        }
        base.OnFrameworkInitializationCompleted();
    }

    // Draws a small rounded "stack of cards" icon so no image file is needed.
    private static WindowIcon CreateIcon()
    {
        var bmp = new RenderTargetBitmap(new PixelSize(64, 64));
        using (var ctx = bmp.CreateDrawingContext())
        {
            var back = new SolidColorBrush(Color.Parse("#8E8E93"));
            var front = new SolidColorBrush(Color.Parse("#0A84FF"));
            ctx.DrawRectangle(back, null, new RoundedRect(new Rect(14, 6, 42, 42), 10));
            ctx.DrawRectangle(front, null, new RoundedRect(new Rect(6, 16, 42, 42), 10));
            var white = new SolidColorBrush(Colors.White);
            ctx.DrawRectangle(white, null, new RoundedRect(new Rect(14, 28, 26, 5), 2.5));
            ctx.DrawRectangle(white, null, new RoundedRect(new Rect(14, 39, 18, 5), 2.5));
        }
        using var ms = new MemoryStream();
        bmp.Save(ms);
        ms.Position = 0;
        return new WindowIcon(ms);
    }
}
