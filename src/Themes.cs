using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;

namespace ClipboardHistory;

public static class Themes
{
    // Accent palettes the user can choose from.
    public static readonly (string Name, string Accent)[] Accents =
    {
        ("Ocean",    "#0A84FF"),
        ("Mint",     "#30D158"),
        ("Sunset",   "#FF9F0A"),
        ("Rose",     "#FF375F"),
        ("Lavender", "#BF5AF2"),
        ("Graphite", "#8E8E93"),
    };

    public static void Apply(Settings s)
    {
        var app = Application.Current!;
        bool dark = s.Mode == "Dark";
        var accentHex = Accents.FirstOrDefault(a => a.Name == s.Theme).Accent ?? Accents[0].Accent;
        var accent = Color.Parse(accentHex);

        app.RequestedThemeVariant = dark ? ThemeVariant.Dark : ThemeVariant.Light;

        void Set(string key, string hex) => app.Resources[key] = new SolidColorBrush(Color.Parse(hex));

        if (dark)
        {
            Set("BgBrush",        "#EE1C1C1E");
            Set("CardBrush",      "#FF2C2C2E");
            Set("CardHoverBrush", "#FF3A3A3C");
            Set("TextBrush",      "#FFF5F5F7");
            Set("SubtleBrush",    "#FF98989D");
            Set("LineBrush",      "#22FFFFFF");
            Set("FieldBrush",     "#FF3A3A3C");
        }
        else
        {
            Set("BgBrush",        "#F2F7F7F9");
            Set("CardBrush",      "#FFFFFFFF");
            Set("CardHoverBrush", "#FFF0F0F3");
            Set("TextBrush",      "#FF1D1D1F");
            Set("SubtleBrush",    "#FF86868B");
            Set("LineBrush",      "#14000000");
            Set("FieldBrush",     "#FFE9E9ED");
        }

        app.Resources["AccentBrush"] = new SolidColorBrush(accent);
        app.Resources["AccentSoftBrush"] = new SolidColorBrush(Color.FromArgb(dark ? (byte)0x40 : (byte)0x26, accent.R, accent.G, accent.B));
        // Make Fluent controls (text box focus, selection) follow the accent too.
        app.Resources["SystemAccentColor"] = accent;
    }
}
