# Windows Development

The repository now includes an experimental native Windows implementation in:

```text
src/windows
```

It is a sibling app to the macOS Swift package, not a rewrite of the existing codebase. The Windows version is an unpackaged WinUI 3 desktop app with a small shared .NET core library for click events, settings, and duplicate filtering.

## Layout

```text
src/windows
├── ClickLight.Windows.slnx
├── ClickLight.Windows.Core
│   ├── Models
│   └── Services
├── ClickLight.Windows
│   ├── Interop
│   ├── Services
│   └── Program.cs
└── build-windows.ps1
```

## What Is Implemented

- shared settings model and persisted Windows settings file
- global low-level mouse hook scaffold for left, right, release, and drag events
- one transparent overlay window per monitor
- animated WinUI 3 pulse rendering for press, release, right-click, and drag
- tray icon menu for enable/disable, visual toggles, presets, test pulse, and quit
- hidden host window for status and debugging

## Local Build

On Windows with the .NET 10 SDK and Windows App SDK prerequisites installed:

```powershell
cd src/windows
./build-windows.ps1
```

This publishes the app to:

```text
src/windows/artifacts/Release/win-x64
```

## Notes

- The Windows app currently uses an unpackaged model to keep tray and local iteration simple.
- The update service is still a stub; shipping auto-update/signing should be added when the Windows distribution path is finalized.
- Linux agents can build the shared core library, but the full WinUI publish/build requires Windows tooling because the Windows App SDK resource generation step runs native Windows executables.
