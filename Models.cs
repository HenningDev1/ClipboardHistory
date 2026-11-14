using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ClipboardHistory;

public sealed class ClipItem : INotifyPropertyChanged
{
    private bool _pinned;

    public string Text { get; set; } = "";
    public DateTime CopiedAt { get; set; } = DateTime.Now;

    public bool Pinned
    {
        get => _pinned;
        set { _pinned = value; OnChanged(); OnChanged(nameof(PinGlyph)); }
    }

    [JsonIgnore] public string Preview =>
        Text.Length > 400 ? Text[..400].Replace("\r", "") + "…" : Text.Replace("\r", "");

    [JsonIgnore] public string PinGlyph => Pinned ? "★" : "☆";

    [JsonIgnore] public string TimeLabel
    {
        get
        {
            var d = DateTime.Now - CopiedAt;
            if (d.TotalSeconds < 60) return "Just now";
            if (d.TotalMinutes < 60) return $"{(int)d.TotalMinutes} min ago";
            if (CopiedAt.Date == DateTime.Today) return CopiedAt.ToString("HH:mm");
            if (CopiedAt.Date == DateTime.Today.AddDays(-1)) return "Yesterday " + CopiedAt.ToString("HH:mm");
            return CopiedAt.ToString("dd MMM, HH:mm");
        }
    }

    public void RefreshTime() => OnChanged(nameof(TimeLabel));

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnChanged([CallerMemberName] string? n = null) => PropertyChanged?.Invoke(this, new(n));
}

public sealed class Settings
{
    public string Theme { get; set; } = "Ocean";
    public string Mode { get; set; } = "Light"; // Light | Dark
    public int MaxItems { get; set; } = 200;
}

public sealed class StoreData
{
    public Settings Settings { get; set; } = new();
    public List<ClipItem> Items { get; set; } = new();
}

public static class Store
{
    private static readonly string Dir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Clipboard History");
    private static readonly string FilePath = Path.Combine(Dir, "history.json");
    private static readonly JsonSerializerOptions Opts = new() { WriteIndented = true };

    public static StoreData Load()
    {
        try
        {
            // Carry history over from the app's old name ("Stash").
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var legacy = new[] { Path.Combine(Dir, "stash.json"), Path.Combine(appData, "Stash", "stash.json") }
                .FirstOrDefault(File.Exists);
            if (!File.Exists(FilePath) && legacy != null)
            {
                Directory.CreateDirectory(Dir);
                File.Copy(legacy, FilePath);
            }
            if (File.Exists(FilePath))
                return JsonSerializer.Deserialize<StoreData>(File.ReadAllText(FilePath), Opts) ?? new();
        }
        catch { /* corrupt file: start fresh */ }
        return new();
    }

    public static void Save(StoreData data)
    {
        try
        {
            Directory.CreateDirectory(Dir);
            var tmp = FilePath + ".tmp";
            File.WriteAllText(tmp, JsonSerializer.Serialize(data, Opts));
            File.Move(tmp, FilePath, true);
        }
        catch { /* ignore disk errors */ }
    }
}
