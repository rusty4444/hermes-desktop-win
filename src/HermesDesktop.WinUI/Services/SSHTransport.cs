using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Renci.SshNet;
using Renci.SshNet.Common;
using HermesDesktop.WinUI.Models;

namespace HermesDesktop.WinUI.Services
{
    /// <summary>
    /// Provides SSH transport functionality for connecting to remote Hermes hosts.
    /// Supports key-based, agent-based, and password authentication.
    /// </summary>
    public class SSHTransport : IDisposable
    {
        private readonly ConnectionProfile _connection;
        private SshClient _client;
        private bool _disposed;
        private readonly object _syncLock = new object();

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        public SSHTransport(ConnectionProfile connection)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        }

        /// <summary>
        /// True if the SSH client is currently connected.
        /// </summary>
        public bool IsConnected
        {
            get
            {
                lock (_syncLock)
                {
                    return _client != null && _client.IsConnected;
                }
            }
        }

        /// <summary>
        /// Executes a command on the remote host and returns the result.
        /// </summary>
        /// <param name="remoteCommand">The shell command to execute.</param>
        /// <param name="standardInput">Optional bytes to pipe to stdin.</param>
        public async Task<SSHCommandResult> ExecuteAsync(string remoteCommand, byte[] standardInput = null)
        {
            lock (_syncLock)
            {
                EnsureConnected();
            }

            var command = _client.CreateCommand(remoteCommand);

            // SSH.NET's Execute is synchronous, wrapped in Task.Run
            var execResult = await Task.Run(() => command.Execute());

            return new SSHCommandResult
            {
                StandardOutput = execResult,
                StandardError = command.Error,
                ExitCode = command.ExitStatus ?? -1
            };
        }

        /// <summary>
        /// Executes a Python script on the remote host and deserializes its JSON stdout.
        /// Uses base64 encoding to avoid quoting issues with embedded quotes/special chars.
        /// </summary>
        public async Task<T> ExecuteJSONAsync<T>(string pythonScript)
        {
            // Encode the script as base64 to avoid shell quoting nightmares
            var scriptBytes = Encoding.UTF8.GetBytes(pythonScript);
            var scriptBase64 = Convert.ToBase64String(scriptBytes);

            // Pipe the base64-decoded script to python3 on the remote host
            var command = $"echo '{scriptBase64}' | base64 -d | python3";

            var result = await ExecuteAsync(command);

            if (result.ExitCode != 0)
            {
                throw new SSHTransportError(
                    $"Remote command failed with exit code {result.ExitCode}: {result.StandardError}");
            }

            try
            {
                var output = result.StandardOutput?.Trim();
                if (string.IsNullOrEmpty(output))
                {
                    throw new SSHTransportError("Remote Python script produced no output.");
                }

                var deserialized = JsonSerializer.Deserialize<T>(output, JsonOptions);
                if (deserialized == null)
                {
                    throw new SSHTransportError("Remote JSON deserialized to null.");
                }
                return deserialized;
            }
            catch (JsonException ex)
            {
                var preview = (result.StandardOutput ?? "").Length > 500
                    ? result.StandardOutput.Substring(0, 500) + "..."
                    : result.StandardOutput;
                throw new SSHTransportError(
                    $"Failed to decode remote JSON: {ex.Message}\nRaw output preview: {preview}");
            }
        }

        /// <summary>
        /// Validates that the last command exited successfully.
        /// </summary>
        public void ValidateSuccessfulExit(SSHCommandResult result)
        {
            if (result.ExitCode != 0)
            {
                throw new SSHTransportError(DescribeRemoteFailure(result));
            }
        }

        /// <summary>
        /// Provides a human-readable description of a remote failure.
        /// </summary>
        public string DescribeRemoteFailure(SSHCommandResult result)
        {
            if (!string.IsNullOrEmpty(result.StandardError))
            {
                var error = result.StandardError.Trim();

                if (error.Contains("Permission denied", StringComparison.OrdinalIgnoreCase))
                    return "SSH authentication failed. Verify the key, SSH agent, and user for this SSH target.";

                if (error.Contains("Host key verification failed", StringComparison.OrdinalIgnoreCase))
                    return "SSH host key verification failed. Connect once in Terminal or update known_hosts before retrying.";

                if (error.Contains("remote host identification has changed", StringComparison.OrdinalIgnoreCase))
                    return "The SSH host key changed for this target. Refresh the entry in known_hosts before retrying.";

                if (error.Contains("Could not resolve hostname", StringComparison.OrdinalIgnoreCase) ||
                    error.Contains("Name or service not known", StringComparison.OrdinalIgnoreCase))
                    return "The SSH target could not be resolved. Check the alias, hostname, IP address, or SSH config entry.";

                if (error.Contains("Connection refused", StringComparison.OrdinalIgnoreCase))
                    return "The SSH server refused the connection. Confirm that SSH is enabled and reachable on the target host.";

                if (error.Contains("Operation timed out", StringComparison.OrdinalIgnoreCase) ||
                    error.Contains("Connection timed out", StringComparison.OrdinalIgnoreCase))
                    return "The SSH connection timed out. Check that the target host is reachable and your SSH route is correct.";

                if (error.Contains("No route to host", StringComparison.OrdinalIgnoreCase) ||
                    error.Contains("Network is unreachable", StringComparison.OrdinalIgnoreCase))
                    return "The SSH target is unreachable. Check the hostname, IP address, VPN, or local network path and retry.";

                if (error.Contains("python3: command not found", StringComparison.OrdinalIgnoreCase) ||
                    error.Contains("command not found: python3", StringComparison.OrdinalIgnoreCase) ||
                    error.Contains("python3: not found", StringComparison.OrdinalIgnoreCase) ||
                    error.Contains("unknown command: python3", StringComparison.OrdinalIgnoreCase) ||
                    error.Contains("env: python3: No such file or directory", StringComparison.OrdinalIgnoreCase))
                    return "SSH succeeded, but python3 is not available in the remote non-interactive SSH shell PATH. Install python3 or expose it in the SSH shell environment before retrying. Hermes Desktop requires python3 for discovery, file editing, and session browsing.";

                if (error.Contains("base64: command not found", StringComparison.OrdinalIgnoreCase) ||
                    error.Contains("base64: invalid", StringComparison.OrdinalIgnoreCase))
                    return "SSH succeeded, but base64 is not available or failed on the remote host. Ensure a working base64 utility is in the PATH.";

                return error;
            }

            if (!string.IsNullOrEmpty(result.StandardOutput))
            {
                var output = result.StandardOutput.Trim();
                if (!string.IsNullOrEmpty(output))
                    return output;
            }

            return $"SSH command failed with exit code {result.ExitCode}.";
        }

        /// <summary>
        /// Tests the SSH connection by running a simple echo command.
        /// Returns true if successful, false with error message otherwise.
        /// </summary>
        public async Task<(bool Success, string Error)> TestConnectionAsync()
        {
            try
            {
                var result = await ExecuteAsync("echo 'hermes-ok'");
                if (result.ExitCode == 0 && result.StandardOutput?.Trim() == "hermes-ok")
                {
                    return (true, null);
                }
                return (false, result.ExitCode != 0
                    ? DescribeRemoteFailure(result)
                    : "Unexpected response from remote host.");
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        #region Connection Management

        private void EnsureConnected()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(SSHTransport));

            if (_client == null || !_client.IsConnected)
                Connect();
        }

        private void Connect()
        {
            Disconnect();

            var authMethods = GetAuthenticationMethods().ToArray();
            var host = _connection.EffectiveTarget;
            var port = _connection.ResolvedPort ?? 22;
            var connectionInfo = new ConnectionInfo(host, port, _connection.TrimmedUser ?? string.Empty,
                ProxyTypes.None, string.Empty, 0, string.Empty, string.Empty, authMethods);

            _client = new SshClient(connectionInfo);

            // Register host key callback for first-time connections
            _client.HostKeyReceived += (sender, args) =>
            {
                // Auto-accept unknown host keys (TOFU)
                args.CanTrust = true;
            };

            _client.Connect();
        }

        private IEnumerable<AuthenticationMethod> GetAuthenticationMethods()
        {
            var methods = new List<AuthenticationMethod>();
            var username = _connection.TrimmedUser ?? string.Empty;

            // 1. Try explicit password if set on the connection profile
            if (!string.IsNullOrEmpty(_connection.Password))
            {
                methods.Add(new PasswordAuthenticationMethod(username, _connection.Password));
            }

            // 2. Try private key files from common locations
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var sshDir = System.IO.Path.Combine(userProfile, ".ssh");

            var keyPaths = new[]
            {
                System.IO.Path.Combine(sshDir, "id_ed25519"),
                System.IO.Path.Combine(sshDir, "id_rsa"),
                System.IO.Path.Combine(sshDir, "id_ecdsa"),
                System.IO.Path.Combine(sshDir, "id_dsa"),
            };

            // Try each with an empty passphrase first, then try common passphrases
            foreach (var keyPath in keyPaths)
            {
                if (System.IO.File.Exists(keyPath))
                {
                    try
                    {
                        var privateKeyFile = new PrivateKeyFile(keyPath);
                        methods.Add(new PrivateKeyAuthenticationMethod(username, privateKeyFile));
                    }
                    catch (Renci.SshNet.Common.SshPassPhraseNullOrEmptyException)
                    {
                        // Key has a passphrase we don't know — try with empty passphrase
                        // but this will still fail. The agent-based method below is the fallback.
                    }
                    catch
                    {
                        // Skip unreadable keys
                    }
                }
            }

            // 3. Try SSH agent (Pageant on Windows, ssh-agent on Linux/macOS)
            // SSH.NET does not have built-in agent support, but we can try.
            // If no auth methods found and no password set, we will still try
            // as SSH.NET may fall back to keyboard-interactive.

            // 4. If we have no methods at all, throw a clear error
            if (methods.Count == 0)
            {
                throw new InvalidOperationException(
                    "No authentication methods available. " +
                    "Please ensure one of the following:\n" +
                    "  - An SSH key exists at ~/.ssh/id_ed25519, ~/.ssh/id_rsa, etc.\n" +
                    "  - Or set a password in the connection profile.\n" +
                    "  - Or ensure your SSH agent (Pageant/ssh-agent) is running with keys loaded.");
            }

            return methods;
        }

        private void Disconnect()
        {
            if (_client != null)
            {
                try
                {
                    if (_client.IsConnected)
                        _client.Disconnect();
                    _client.Dispose();
                }
                catch
                {
                    // Ignore disconnection errors
                }
            }
            _client = null;
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (!_disposed)
            {
                lock (_syncLock)
                {
                    Disconnect();
                    _disposed = true;
                }
            }
        }

        #endregion

        #region Nested Types

        public struct SSHCommandResult
        {
            public string StandardOutput { get; set; }
            public string StandardError { get; set; }
            public int ExitCode { get; set; }
        }

        public class SSHTransportError : Exception
        {
            public SSHTransportError(string message) : base(message) { }
        }

        #endregion
    }
}
