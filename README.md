# Clipboard History

A small, clean clipboard manager for Windows. It sits in your system tray, remembers everything you copy, and lets you find it again in a second.

## Features

- **Remembers what you copy:** every piece of text you copy is saved automatically
- **One click to copy again:** click an item and it's back on your clipboard
- **Instant search:** start typing to filter your whole history
- **Pinned snippets:** star the things you paste often so they stay at the top and are never cleared
- **Copy times:** each item shows when you copied it ("Just now", "5 min ago", "Yesterday 09:10")
- **Themes:** light and dark mode with six accent colours
- **Keyboard friendly:** open it from anywhere with `Ctrl+Shift+V`. `Enter` copies the top result and `Esc` closes it
- **Lightweight and private:** no account, no internet connection, no telemetry. Your history stays on your PC

## Download

Download **`ClipboardHistorySetup.exe`** from the [latest release](https://github.com/HenningDev1/ClipboardHistory/releases/latest) and run it.

- No admin rights needed: it installs just for your user account
- Adds a Start Menu entry so you can find it with Windows search
- Can start automatically with Windows
- Can be uninstalled from **Settings → Apps**

Prefer not to install? `ClipboardHistory.exe` runs on its own as a portable app.

> Windows SmartScreen may warn you the first time because the app isn't code-signed. Click **More info → Run anyway**.

## Usage

| Action | How |
|---|---|
| Open | Click the tray icon, or press `Ctrl+Shift+V` |
| Copy an item | Click it (or press `Enter` for the top result) |
| Pin / unpin | Click the ☆ on an item |
| Remove an item | Click the ✕ on an item |
| Settings | Click ⚙ for theme, accent colour, history size and clearing history |
| Quit | Right-click the tray icon → Quit |

Your history is saved in `%APPDATA%\Clipboard History\history.json`.

## Requirements

- Windows 10 or 11 (64-bit)
- Nothing else: the .NET runtime is included

## Building from source

You need the [.NET 10 SDK](https://dotnet.microsoft.com/download).

```bash
cd src
dotnet run -c Release
```

To build the installer:

```bash
cd src
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:DebugType=none -o publish
```

Then rename `publish/ClipboardHistory.exe` to `ClipboardHistorySetup.exe`. When the exe's name contains "Setup" it opens the installer; under any other name it runs the app itself. `--install` installs silently and `--uninstall` removes the app.

Built with C# and [Avalonia UI](https://avaloniaui.net/).
