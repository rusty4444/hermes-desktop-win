# Hermes Desktop for Windows

This is a port of the original Hermes Desktop macOS app to Windows using WinUI and .NET.

## Project Structure

- `src/HermesDesktop.WinUI`: The main WinUI application project.
- `src/HermesDesktop.WinUI/Models`: Data models (e.g., ConnectionProfile).
- `src/HermesDesktop.WinUI/Services`: Service implementations (e.g., SSHTransport).
- `src/HermesDesktop.WinUI/ViewModels`: MVVM view models (to be implemented).
- `src/HermesDesktop.WinUI.Views`: UI views (to be implemented).

## Dependencies

- [Windows App SDK](https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/)
- [SSH.NET](https://sshnet.codeplex.com/) for SSH communications.

## Building

To build this project, you need:
1. Windows 10 version 1809 (10.0.17763) or later.
2. Windows App SDK 1.5 or later.
3. .NET SDK 8.0 or later.

Then, from the root of this repository:
```
dotnet build src/HermesDesktop.WinUI/HermesDesktop.WinUI.csproj
```

## Current Status

This port is a work in progress. The following has been completed:
- Project setup for WinUI.
- Basic application and window structure.
- SSH transport layer using SSH.NET.
- Connection profile model.

The next steps are to implement the various services and views to match the functionality of the original macOS app.

## Note

The original Hermes Desktop app is an SSH-native macOS application that connects to a Hermes host over SSH. This port aims to provide the same functionality on Windows by using SSH.NET for SSH communications and WinUI for the user interface.

Due to the differences in platform capabilities and UI frameworks, some features may require adjustments or may not be directly portable.

