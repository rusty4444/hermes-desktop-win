using System;
using HermesDesktop.WinUI.Models;
using HermesDesktop.WinUI.Services;

namespace HermesDesktop.WinUI
{
    /// <summary>
    /// Holds the application state, including the active connection and services.
    /// </summary>
    public class AppState
    {
        private static readonly Lazy<AppState> lazy = new Lazy<AppState>(() => new AppState());
        public static AppState Instance => lazy.Value;

        public ConnectionProfile ActiveConnection { get; set; } = new ConnectionProfile();
        public SSHTransport SshTransport { get; private set; }
        public SessionBrowserService SessionBrowserService { get; private set; }
        public WorkflowService WorkflowService { get; private set; }
        public KanbanService KanbanService { get; private set; }
        public FileEditorService FileEditorService { get; private set; }
        public UsageService UsageService { get; private set; }
        public SkillService SkillService { get; private set; }
        public TerminalService TerminalService { get; private set; }

        private AppState()
        {
            // Initialize the SSH transport with the active connection (which may be empty initially)
            SshTransport = new SSHTransport(new AppPaths());
            SessionBrowserService = new SessionBrowserService(SshTransport);
            WorkflowService = new WorkflowService();
            KanbanService = new KanbanService(SshTransport);
            FileEditorService = new FileEditorService(SshTransport);
            UsageService = new UsageService(SshTransport);
            SkillService = new SkillService(SshTransport);
            // TerminalService will be created on demand when needed
        }

        // We'll add more services as needed, but for now, we just have the SSH transport.
    }

    // A placeholder for AppPaths, similar to the Swift version.
    public class AppPaths
    {
        public string ControlPathFor(ConnectionProfile connection)
        {
            // In the Swift version, this returns a path for the SSH control socket.
            // We are not implementing multiplexing in the SSH.NET version for simplicity.
            // Return an empty string or a dummy path.
            return string.Empty;
        }
    }
}
