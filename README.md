# Clipboard History

A small Windows tray app that keeps your clipboard history.

- Click the tray icon or press **Ctrl+Shift+V** to open it
- Click an item to copy it again (Enter copies the top result, Esc closes)
- Search, pin snippets with the star, and choose light/dark mode plus an accent color in settings

History is saved to `%APPDATA%\Clipboard History\history.json`.

```bash
dotnet run -c Release
```


## Installer

Build `publish\ClipboardHistorySetup.exe` (run it to install; `--install` installs silently):

```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:DebugType=none -o publish; mv publish/ClipboardHistory.exe publish/ClipboardHistorySetup.exe
```

Installs per user to `%LOCALAPPDATA%\Programs\Clipboard History` with a Start Menu entry and an uninstaller in Settings > Apps.

