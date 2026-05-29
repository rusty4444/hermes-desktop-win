using System;

namespace HermesDesktop.WinUI.Models
{
    /// <summary>
    /// Represents an SSH connection profile.
    /// </summary>
    public class ConnectionProfile
    {
        public string Host { get; set; } = string.Empty;
        public string User { get; set; } = string.Empty;
        public int? Port { get; set; }
        public string SshAlias { get; set; } = string.Empty;
        public string HermesProfile { get; set; } = string.Empty;

        /// <summary>
        /// Gets the effective target (hostname or IP) for the SSH connection.
        /// </summary>
        public string EffectiveTarget =>
            !string.IsNullOrEmpty(SshAlias) ? SshAlias :
            !string.IsNullOrEmpty(Host) ? Host :
            string.Empty;

        /// <summary>
        /// Gets the trimmed user (if set) for the SSH connection.
        /// </summary>
        public string TrimmedUser =>
            !string.IsNullOrEmpty(User) ? User.Trim() : null;

        /// <summary>
        /// Gets the resolved port (if set) for the SSH connection.
        /// </summary>
        public int? ResolvedPort => Port;

        /// <summary>
        /// Validates the connection profile and returns an error message if invalid.
        /// </summary>
        public string SSHValidationError
        {
            get
            {
                if (string.IsNullOrEmpty(EffectiveTarget))
                {
                    return "SSH target (host or alias) is required.";
                }

                // Additional validation can be added here.

                return null;
            }
        }

        /// <summary>
        /// Gets the remote service command for the given command.
        /// In the original Swift code, this was used to run a command on the remote host.
        /// We are simplifying: just return the command.
        /// </summary>
        public string RemoteServiceCommand(string command) => command;

        /// <summary>
        /// Gets the remote shell bootstrap command for the given startup command line.
        /// In the original Swift code, this was used to start a shell with an optional command.
        /// We are simplifying: just return the startup command line or an empty string.
        /// </summary>
        public string RemoteShellBootstrapCommand(string startupCommandLine) =>
            string.IsNullOrEmpty(startupCommandLine) ? string.Empty : startupCommandLine;
    }
}
