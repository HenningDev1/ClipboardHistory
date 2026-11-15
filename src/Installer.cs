using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace ClipboardHistory;

/// <summary>Per-user install/uninstall: no admin rights needed.</summary>
public static class Installer
{
    public const string AppName = "Clipboard History";
    public const string Version = "1.0.0";
    private const string ExeName = "ClipboardHistory.exe";
    private const string UninstallRoot = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\";
    private const string UninstallKey = UninstallRoot + "ClipboardHistory";

    public static string InstallDir =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", AppName);
    public static string InstalledExe => Path.Combine(InstallDir, ExeName);
    private static string StartMenuLink =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), AppName + ".lnk");
    private static string StartupLink =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), AppName + ".lnk");
    private static string DataDir =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), AppName);

    /// <summary>True when this exe was launched as the setup program.</summary>
    public static bool IsSetupMode(string[] args)
    {
        if (args.Contains("--setup")) return true;
        var name = Path.GetFileNameWithoutExtension(Environment.ProcessPath ?? "");
        return name.Contains("setup", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsInstalled => File.Exists(InstalledExe);
    public static bool StartsWithWindows => File.Exists(StartupLink) || File.Exists(LegacyStartupLink);

    public static void Install(bool runAtStartup)
    {
        KillOtherInstances();
        RemoveLegacyStash();
        Directory.CreateDirectory(InstallDir);
        File.Copy(Environment.ProcessPath!, InstalledExe, true);

        CreateShortcut(StartMenuLink, InstalledExe, "Clipboard history with search");
        if (runAtStartup) CreateShortcut(StartupLink, InstalledExe, "Clipboard history with search");
        else TryDelete(StartupLink);

        using var key = Registry.CurrentUser.CreateSubKey(UninstallKey);
        key.SetValue("DisplayName", AppName);
        key.SetValue("DisplayVersion", Version);
        key.SetValue("Publisher", AppName);
        key.SetValue("DisplayIcon", InstalledExe);
        key.SetValue("InstallLocation", InstallDir);
        key.SetValue("UninstallString", $"\"{InstalledExe}\" --uninstall");
        key.SetValue("NoModify", 1, RegistryValueKind.DWord);
        key.SetValue("NoRepair", 1, RegistryValueKind.DWord);
        key.SetValue("EstimatedSize", (int)(new FileInfo(InstalledExe).Length / 1024), RegistryValueKind.DWord);
    }

    public static void LaunchInstalled() =>
        Process.Start(new ProcessStartInfo(InstalledExe) { UseShellExecute = true, WorkingDirectory = InstallDir });

    public static void Uninstall()
    {
        if (MessageBoxW(IntPtr.Zero, $"Remove {AppName} from this PC?\n\nYour saved clipboard history will be deleted too.",
                $"Uninstall {AppName}", 0x24 /* YESNO | QUESTION */) != 6) return;

        KillOtherInstances();
        TryDelete(StartMenuLink);
        TryDelete(StartupLink);
        try { Registry.CurrentUser.DeleteSubKeyTree(UninstallKey, false); } catch { }
        try { if (Directory.Exists(DataDir)) Directory.Delete(DataDir, true); } catch { }

        // The running exe can't delete itself: let cmd do it after we exit.
        Process.Start(new ProcessStartInfo("cmd.exe",
            $"/c timeout /t 2 /nobreak >nul & rmdir /s /q \"{InstallDir}\"")
        { CreateNoWindow = true, UseShellExecute = false, WorkingDirectory = Path.GetTempPath() });

        MessageBoxW(IntPtr.Zero, $"{AppName} has been removed.", $"Uninstall {AppName}", 0x40);
    }

    // ---- Clean-up of the earlier version, which was called "Stash" ----

    private static string LegacyStartupLink =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "Stash.lnk");

    private static void RemoveLegacyStash()
    {
        TryDelete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "Stash.lnk"));
        TryDelete(LegacyStartupLink);
        try { Registry.CurrentUser.DeleteSubKeyTree(UninstallRoot + "Stash", false); } catch { }
        try
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Stash");
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
        }
        catch { }
        // The old history file (%APPDATA%\Stash) is kept; the app copies it over on first start.
    }

    private static void KillOtherInstances()
    {
        foreach (var name in new[] { "ClipboardHistory", "Stash" })
        foreach (var p in Process.GetProcessesByName(name))
        {
            if (p.Id == Environment.ProcessId) continue;
            try { p.Kill(); p.WaitForExit(3000); } catch { }
        }
    }

    private static void CreateShortcut(string linkPath, string target, string description)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(linkPath)!);
        var shellType = Type.GetTypeFromProgID("WScript.Shell")!;
        dynamic shell = Activator.CreateInstance(shellType)!;
        try
        {
            dynamic link = shell.CreateShortcut(linkPath);
            link.TargetPath = target;
            link.WorkingDirectory = Path.GetDirectoryName(target);
            link.Description = description;
            link.IconLocation = target + ",0";
            link.Save();
            Marshal.FinalReleaseComObject(link);
        }
        finally { Marshal.FinalReleaseComObject(shell); }
    }

    private static void TryDelete(string path) { try { if (File.Exists(path)) File.Delete(path); } catch { } }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int MessageBoxW(IntPtr hWnd, string text, string caption, uint type);
}
