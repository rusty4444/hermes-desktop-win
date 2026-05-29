# Hermes Desktop for Windows

A WinUI 3 port of [hermes-desktop](https://github.com/dodo-reach/hermes-desktop) — an SSH-native desktop client for managing your remote [Hermes](https://github.com/NousResearch/Hermes) agent. Connect to any Linux/macOS host running Hermes and manage sessions, chat, skills, cron jobs, files, and more — all through a native Windows GUI over SSH.

## Features

| Section | Description |
|---|---|
| **Chat** | Interactive chat with your Hermes agent — send prompts, resume sessions, toggle auto-approve |
| **Overview** | Connection status, workspace discovery, available profiles |
| **Sessions** | Browse past sessions, view full transcripts, copy/export |
| **Workflows** | Create and manage preset workflows with skills and prompts |
| **Cron Jobs** | List, run, pause, resume, and delete recurring cron jobs |
| **Kanban** | Visual Kanban board with lanes and task cards |
| **Files** | Remote file browser with monospace text editor |
| **Usage** | Usage statistics — total sessions, messages, tokens, top models |
| **Skills** | Browse, edit, create, and delete skill markdown files |
| **Terminal** | Embedded SSH terminal with dark theme |
| **Settings** | Connection profile management — alias, host, user, port, password, Hermes profile |

## Requirements

### Development
- **Windows 10** version 1809 (build 17763) or later, or **Windows 11**
- **.NET 8 SDK** (8.0.x)
- **Windows App SDK** 1.5

### Runtime
- **Windows 10** version 1809+ or **Windows 11**
- **SSH access** to a Linux/macOS host running Hermes
- **Python 3** on the remote host (used for service discovery, file editing, session browsing)
- `hermes` CLI binary available on the remote host's PATH

## Quick Start

### Download (Windows)

Get the latest `.exe` from [Releases](https://github.com/rusty4444/hermes-desktop-win/releases).

### Build from Source

```powershell
git clone https://github.com/rusty4444/hermes-desktop-win.git
cd hermes-desktop-win
dotnet restore src/HermesDesktop.WinUI/HermesDesktop.WinUI.csproj
dotnet build src/HermesDesktop.WinUI/HermesDesktop.WinUI.csproj -c Release
```

The built executable will be at:
```
src/HermesDesktop.WinUI/bin/Release/net8.0-windows10.0.19041.0/HermesDesktop.WinUI.exe
```

### Publish Self-Contained (no .NET runtime needed)

```powershell
dotnet publish src/HermesDesktop.WinUI/HermesDesktop.WinUI.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish
```

## Setup

1. **Launch** HermesDesktop.WinUI.exe
2. Go to **Settings** and configure a connection profile:
   - **Alias** — SSH alias (e.g., `hermes-host`)
   - **Host** — Hostname or IP address
   - **User** — SSH username
   - **Port** — SSH port (default: 22)
   - **Password** — Optional SSH password (key-based auth preferred)
   - **Hermes Profile** — Hermes profile name (leave blank for default)
   - **Hermes Home** — Custom Hermes home path (leave blank for `~/.hermes`)
3. Click **Add** to save
4. Select the profile and click **Set as Active**
5. Navigate to **Chat** and start chatting with your Hermes agent

## Authentication

The app supports two authentication methods per connection profile:

1. **SSH Key** — Place your private key at `~/.ssh/id_ed25519`, `~/.ssh/id_rsa`, or `~/.ssh/id_ecdsa`
2. **Password** — Enter password directly in the Settings form

The app will try all available methods. Key-based auth is preferred.

## Remote Host Requirements

Your SSH target must have:
- **Python 3** available in the non-interactive SSH PATH
- **Hermes CLI** available — install with `pip install hermes-cli`
- **Standard Unix tools**: `base64`, `echo`

## Architecture

```
HermesDesktop.WinUI (WinUI 3, .NET 8)
├── Models/          Data models (ConnectionProfile, SessionInfo, etc.)
├── Services/        13 service classes
│   ├── SSHTransport          SSH via SSH.NET (base64-scripted Python)
│   ├── HermesChatService     Chat via `hermes chat --query`
│   ├── SessionBrowserService Session listing + transcripts
│   ├── RemoteHermesService   Workspace discovery
│   ├── CronBrowserService    Cron job management
│   ├── KanbanService         Kanban board CRUD
│   ├── FileEditorService     Remote file browse/edit/delete
│   ├── SkillService          Skill file management
│   ├── UsageService          Usage statistics
│   ├── WorkflowService       Local workflow presets
│   ├── TerminalService       SSH shell stream
│   └── ConnectionProfileService Profile persistence
├── ViewModels/      11 MVVM view models
├── Views/           11 XAML pages
├── Converters/      9 value converters
└── AppState.cs      Singleton service container
```

All remote operations use SSH. The app sends Python scripts (base64-encoded) to the host which run via `python3` and return JSON. No persistent daemon or API server is required on the remote host — just SSH + Python.

## Project Structure

```
hermes-desktop-win/
├── .github/workflows/build.yml   CI: build + release artifact upload
├── src/
│   └── HermesDesktop.WinUI/
│       ├── HermesDesktop.WinUI.csproj
│       ├── AppState.cs
│       ├── MainWindow.xaml/.cs
│       ├── Models/
│       │   ├── ConnectionProfile.cs
│       │   └── Models.cs          (all shared model types)
│       ├── Services/              (13 services)
│       ├── ViewModels/            (11 view models)
│       ├── Views/                 (11 XAML pages)
│       └── Converters/            (9 converters)
├── LICENSE
└── README.md
```

## Limitations

- **Cannot be built on macOS/Linux** — requires Windows SDK
- Chat responses are collected in full before display (no streaming)
- Kanban uses a simple JSON file format (not the SQLite kanban.db)
- No update-check service (macOS uses Sparkle)
- Terminal uses synchronous read polling (not true PTY streaming)

## License

MIT — see [LICENSE](LICENSE)

## Acknowledgments

This is a port of the original [hermes-desktop](https://github.com/dodo-reach/hermes-desktop) macOS app by [dodo-reach](https://github.com/dodo-reach). The original uses Swift/SwiftUI with native `ssh` process invocation; this port uses C#/WinUI 3 with SSH.NET.
