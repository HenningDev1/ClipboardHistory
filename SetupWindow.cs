using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace ClipboardHistory;

/// <summary>Small installer window shown when the exe runs as a setup exe.</summary>
public sealed class SetupWindow : Window
{
    public SetupWindow()
    {
        Title = "Install Clipboard History";
        Width = 420; Height = 330;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        FontFamily = new FontFamily("avares://Avalonia.Fonts.Inter/Assets#Inter");
        this[!BackgroundProperty] = this.GetResourceObservable("BgBrush").ToBinding();

        IBrush Res(string k) => (IBrush)Application.Current!.Resources[k]!;

        var icon = new Border
        {
            Width = 56, Height = 56, CornerRadius = new CornerRadius(14),
            Background = Res("AccentBrush"), HorizontalAlignment = HorizontalAlignment.Left,
            Child = new TextBlock
            {
                Text = "C", FontSize = 28, FontWeight = FontWeight.Bold, Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center,
            },
        };

        var title = new TextBlock { Text = "Clipboard History", FontSize = 24, FontWeight = FontWeight.Bold, Foreground = Res("TextBrush"), Margin = new Thickness(0, 14, 0, 2) };
        var sub = new TextBlock
        {
            Text = Installer.IsInstalled ? "Clipboard History is already installed. Install again to update it."
                                         : "Clipboard history with search and pinned snippets.",
            FontSize = 13, Foreground = Res("SubtleBrush"), TextWrapping = TextWrapping.Wrap,
        };

        var startup = new CheckBox
        {
            Content = "Start Clipboard History when Windows starts", Margin = new Thickness(0, 18, 0, 0),
            IsChecked = !Installer.IsInstalled || Installer.StartsWithWindows, Foreground = Res("TextBrush"),
        };

        var where = new TextBlock
        {
            Text = "Installs to " + Installer.InstallDir, FontSize = 11, Margin = new Thickness(0, 6, 0, 0),
            Foreground = Res("SubtleBrush"), TextTrimming = TextTrimming.CharacterEllipsis,
        };

        var status = new TextBlock { FontSize = 12.5, Foreground = Res("SubtleBrush"), VerticalAlignment = VerticalAlignment.Center };

        var install = new Button
        {
            Content = "Install", Background = Res("AccentBrush"), Foreground = Brushes.White,
            CornerRadius = new CornerRadius(9), Padding = new Thickness(26, 8), FontWeight = FontWeight.SemiBold,
            HorizontalAlignment = HorizontalAlignment.Right,
        };
        var cancel = new Button
        {
            Content = "Cancel", Background = Res("FieldBrush"), Foreground = Res("TextBrush"),
            CornerRadius = new CornerRadius(9), Padding = new Thickness(18, 8), Margin = new Thickness(0, 0, 8, 0),
        };
        cancel.Click += (_, _) => Close();

        bool done = false;
        install.Click += async (_, _) =>
        {
            if (done) { Installer.LaunchInstalled(); Close(); return; }
            install.IsEnabled = cancel.IsEnabled = startup.IsEnabled = false;
            status.Text = "Installing…";
            try
            {
                bool runAtStartup = startup.IsChecked == true;
                await Task.Run(() => Installer.Install(runAtStartup));
                done = true;
                status.Text = "✓ Installed. Find it by searching \"Clipboard History\".";
                install.Content = "Open Clipboard History";
                cancel.Content = "Close";
                install.IsEnabled = cancel.IsEnabled = true;
            }
            catch (Exception ex)
            {
                status.Text = "Install failed: " + ex.Message;
                install.IsEnabled = cancel.IsEnabled = startup.IsEnabled = true;
            }
        };

        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        buttons.Children.Add(cancel);
        buttons.Children.Add(install);

        var footer = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), VerticalAlignment = VerticalAlignment.Bottom };
        footer.Children.Add(status);
        Grid.SetColumn(buttons, 1);
        footer.Children.Add(buttons);

        var top = new StackPanel();
        top.Children.Add(icon);
        top.Children.Add(title);
        top.Children.Add(sub);
        top.Children.Add(startup);
        top.Children.Add(where);

        var root = new Grid { Margin = new Thickness(28, 26), RowDefinitions = new RowDefinitions("*,Auto") };
        root.Children.Add(top);
        Grid.SetRow(footer, 1);
        root.Children.Add(footer);
        Content = root;
    }
}
