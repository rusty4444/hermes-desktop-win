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

        private ConnectionProfile _activeConnection = new ConnectionProfile();
        public ConnectionProfile ActiveConnection
        {
            get => _activeConnection;
            set
            {
                _activeConnection = value;
                InitializeServices();
            }
        }

        public SSHTransport SshTransport { get; private set; }
        public SessionBrowserService SessionBrowserService { get; private set; }
        public WorkflowService WorkflowService { get; private set; }
        public KanbanService KanbanService { get; private set; }
        public FileEditorService FileEditorService { get; private set; }
        public UsageService UsageService { get; private set; }
        public SkillService SkillService { get; private set; }
        public TerminalService TerminalService { get; private set; }
        public CronBrowserService CronBrowserService { get; private set; }
        public RemoteHermesService RemoteHermesService { get; private set; }
        public HermesChatService HermesChatService { get; private set; }

        private AppState()
        {
            InitializeServices();
        }

        private void InitializeServices()
        {
            // Initialize the SSH transport with the active connection
            SshTransport = new SSHTransport(ActiveConnection);
            // Initialize services that depend on SSH transport
            SessionBrowserService = new SessionBrowserService(SshTransport);
            WorkflowService = new WorkflowService(); // This one does not depend on SSH
            KanbanService = new KanbanService(SshTransport);
            FileEditorService = new FileEditorService(SshTransport);
            UsageService = new UsageService(SshTransport);
            SkillService = new SkillService(SshTransport);
            CronBrowserService = new CronBrowserService(SshTransport);
            RemoteHermesService = new RemoteHermesService(SshTransport);
            HermesChatService = new HermesChatService(SshTransport);
            // TerminalService will be created on demand when needed
        }
    }
}
