using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Transformation;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace ClipboardHistory;

public partial class MainWindow : Window
{
    private readonly StoreData _data;
    private readonly DispatcherTimer _saveTimer;
    private readonly DispatcherTimer _clockTimer;
    private string? _lastText;
    private bool _pinnedOnly;
    private DateTime _shownAt;

    public ObservableCollection<ClipItem> Filtered { get; } = new();

    // Win32
    private const int WM_CLIPBOARDUPDATE = 0x031D, WM_HOTKEY = 0x0312, HOTKEY_ID = 0x5354;
    private const uint MOD_CONTROL = 0x2, MOD_SHIFT = 0x4, MOD_NOREPEAT = 0x4000, VK_V = 0x56;
    [DllImport("user32.dll")] private static extern bool AddClipboardFormatListener(IntPtr hwnd);
    [DllImport("user32.dll")] private static extern bool RegisterHotKey(IntPtr hwnd, int id, uint mods, uint vk);

    public MainWindow() : this(new StoreData()) { }

    public MainWindow(StoreData data)
    {
        _data = data;
        InitializeComponent();
        DataContext = this;

        _saveTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _saveTimer.Tick += (_, _) => { _saveTimer.Stop(); SaveNow(); };
        _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
        _clockTimer.Tick += (_, _) => { foreach (var i in Filtered) i.RefreshTime(); };

        _lastText = _data.Items.FirstOrDefault()?.Text;

        SearchBox.TextChanged += (_, _) => Refresh();
        Header.PointerPressed += (_, e) => { if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) BeginMoveDrag(e); };
        Deactivated += (_, _) => { if (DateTime.Now - _shownAt > TimeSpan.FromMilliseconds(300)) HidePopup(); };
        KeyDown += OnKey;

        BuildSettings();
        Refresh();
        HookWin32();
    }

    // ---------- Win32 clipboard listener + global hotkey ----------

    private void HookWin32()
    {
        var hwnd = TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
        if (hwnd == IntPtr.Zero) return;

        Win32Properties.AddWndProcHookCallback(this, (IntPtr h, uint msg, IntPtr w, IntPtr l, ref bool handled) =>
        {
            if (msg == WM_CLIPBOARDUPDATE) Dispatcher.UIThread.Post(ReadClipboard);
            else if (msg == WM_HOTKEY && (int)w == HOTKEY_ID) { Dispatcher.UIThread.Post(Toggle); handled = true; }
            return IntPtr.Zero;
        });
        AddClipboardFormatListener(hwnd);
        RegisterHotKey(hwnd, HOTKEY_ID, MOD_CONTROL | MOD_SHIFT | MOD_NOREPEAT, VK_V);
    }

    private async void ReadClipboard()
    {
        try
        {
            if (Clipboard is null) return;
            string? text = null;
            for (int attempt = 0; attempt < 3 && text is null; attempt++)
            {
                try { text = await Clipboard.GetTextAsync(); }
                catch { await Task.Delay(60); } // clipboard busy in another app
            }
            if (string.IsNullOrWhiteSpace(text) || text == _lastText) return;
            _lastText = text;
            AddOrBump(text);
        }
        catch { /* never crash on clipboard */ }
    }

    private void AddOrBump(string text)
    {
        var existing = _data.Items.FirstOrDefault(i => i.Text == text);
        if (existing != null) _data.Items.Remove(existing);
        var item = existing ?? new ClipItem { Text = text };
        item.CopiedAt = DateTime.Now;
        _data.Items.Insert(0, item);
        Trim();
        Refresh();
        QueueSave();
    }

    private void Trim()
    {
        var max = Math.Max(10, _data.Settings.MaxItems);
        while (_data.Items.Count(i => !i.Pinned) > max)
        {
            var oldest = _data.Items.Last(i => !i.Pinned);
            _data.Items.Remove(oldest);
        }
    }

    // ---------- List ----------

    private void Refresh()
    {
        var q = SearchBox.Text?.Trim() ?? "";
        var items = _data.Items.Where(i =>
            (!_pinnedOnly || i.Pinned) &&
            (q.Length == 0 || i.Text.Contains(q, StringComparison.OrdinalIgnoreCase)));
        // Pinned snippets float to the top.
        var list = items.OrderByDescending(i => i.Pinned).ToList();

        Filtered.Clear();
        foreach (var i in list) { i.RefreshTime(); Filtered.Add(i); }

        int pinned = _data.Items.Count(i => i.Pinned);
        CountText.Text = $"{_data.Items.Count} items · {pinned} pinned";
        EmptyText.Text = q.Length > 0 ? "No matches." :
            _pinnedOnly ? "No pinned snippets yet.\nClick ☆ on an item to pin it." :
            "Nothing here yet.\nCopy some text and it will show up.";
        EmptyText.IsVisible = Filtered.Count == 0 && !SettingsPanel.IsVisible;
    }

    private async void OnCardReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (e.InitialPressMouseButton != MouseButton.Left) return;
        if ((e.Source as Visual)?.FindAncestorOfType<Button>(true) != null) return;
        if (sender is not Control { DataContext: ClipItem item } || Clipboard is null) return;

        _lastText = item.Text; // don't re-record our own copy as new
        await Clipboard.SetTextAsync(item.Text);
        item.CopiedAt = DateTime.Now;
        _data.Items.Remove(item);
        _data.Items.Insert(0, item);
        QueueSave();

        Toast.Opacity = 1;
        await Task.Delay(550);
        Toast.Opacity = 0;
        HidePopup();
        Refresh();
    }

    private void OnPinClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Control { DataContext: ClipItem item })
        {
            item.Pinned = !item.Pinned;
            Refresh();
            QueueSave();
        }
    }

    private void OnDeleteClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Control { DataContext: ClipItem item })
        {
            _data.Items.Remove(item);
            Refresh();
            QueueSave();
        }
    }

    private void OnTabClick(object? sender, RoutedEventArgs e)
    {
        _pinnedOnly = sender == PinnedTab;
        AllTab.IsChecked = !_pinnedOnly;
        PinnedTab.IsChecked = _pinnedOnly;
        ShowSettings(false);
        Refresh();
    }

    // ---------- Settings ----------

    private static readonly int[] MaxOptions = { 50, 100, 200, 500, 1000 };

    private void BuildSettings()
    {
        foreach (var (name, hex) in Themes.Accents)
        {
            var b = new Button { Background = new SolidColorBrush(Color.Parse(hex)), Tag = name };
            b.Classes.Add("swatch");
            ToolTip.SetTip(b, name);
            b.Click += (_, _) => { _data.Settings.Theme = name; ApplyTheme(); };
            Swatches.Children.Add(b);
        }
        foreach (var m in MaxOptions) MaxItemsBox.Items.Add(m);
        MaxItemsBox.SelectedItem = MaxOptions.Contains(_data.Settings.MaxItems) ? _data.Settings.MaxItems : 200;
        UpdateSettingsUi();
    }

    private void UpdateSettingsUi()
    {
        foreach (var b in Swatches.Children.OfType<Button>())
            b.Classes.Set("selected", (string?)b.Tag == _data.Settings.Theme);
        ThemeName.Text = _data.Settings.Theme;
        LightMode.IsChecked = _data.Settings.Mode != "Dark";
        DarkMode.IsChecked = _data.Settings.Mode == "Dark";
    }

    private void ApplyTheme()
    {
        Themes.Apply(_data.Settings);
        UpdateSettingsUi();
        QueueSave();
    }

    private void OnModeClick(object? sender, RoutedEventArgs e)
    {
        _data.Settings.Mode = sender == DarkMode ? "Dark" : "Light";
        ApplyTheme();
    }

    private void OnMaxItemsChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (MaxItemsBox.SelectedItem is int n && n != _data.Settings.MaxItems)
        {
            _data.Settings.MaxItems = n;
            Trim();
            Refresh();
            QueueSave();
        }
    }

    private void OnClearClick(object? sender, RoutedEventArgs e)
    {
        _data.Items.RemoveAll(i => !i.Pinned);
        Refresh();
        QueueSave();
    }

    private void OnSettingsClick(object? sender, RoutedEventArgs e) => ShowSettings(!SettingsPanel.IsVisible);

    private void ShowSettings(bool show)
    {
        SettingsPanel.IsVisible = show;
        ListView.IsVisible = !show;
        SearchBox.IsEnabled = !show;
        SettingsButton.Classes.Set("on", show);
        if (!show) Refresh(); else EmptyText.IsVisible = false;
    }

    // ---------- Show / hide ----------

    public void Toggle()
    {
        if (IsVisible) HidePopup(); else ShowPopup();
    }

    public void ShowPopup()
    {
        // Sit above the tray in the bottom-right corner of the work area.
        var screen = Screens.Primary ?? Screens.All.FirstOrDefault();
        if (screen != null)
        {
            var wa = screen.WorkingArea;
            double s = screen.Scaling;
            Position = new PixelPoint(
                wa.Right - (int)(Width * s) - (int)(8 * s),
                wa.Bottom - (int)(Height * s) - (int)(8 * s));
        }

        ShowSettings(false);
        SearchBox.Text = "";
        Refresh();
        _shownAt = DateTime.Now;

        Shell.Opacity = 0;
        Shell.RenderTransform = TransformOperations.Parse("translateY(14px) scale(0.98)");
        Show();
        Activate();
        _clockTimer.Start();
        Dispatcher.UIThread.Post(() =>
        {
            Shell.Opacity = 1;
            Shell.RenderTransform = TransformOperations.Parse("translateY(0px) scale(1)");
            SearchBox.Focus();
        }, DispatcherPriority.Background);
    }

    public void HidePopup()
    {
        if (!IsVisible) return;
        _clockTimer.Stop();
        Hide();
    }

    private void OnKey(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape) { HidePopup(); e.Handled = true; }
        else if (e.Key == Key.Enter && Filtered.Count > 0 && Clipboard != null)
        {
            // Enter copies the top result.
            var item = Filtered[0];
            _lastText = item.Text;
            _ = Clipboard.SetTextAsync(item.Text);
            HidePopup();
            e.Handled = true;
        }
    }

    // ---------- Persistence ----------

    private void QueueSave() { _saveTimer.Stop(); _saveTimer.Start(); }
    public void SaveNow() => Store.Save(_data);
}
