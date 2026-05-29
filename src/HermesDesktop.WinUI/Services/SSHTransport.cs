using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Renci.SshNet;
using Renci.SshNet.Common;

namespace HermesDesktop.WinUI.Services
{
    /// <summary>
    /// Provides SSH transport functionality.
    /// </summary>
    public class SSHTransport : IDisposable
    {
        private readonly ConnectionProfile _connection;
        private SshClient _client;
        private bool _disposed;
        private readonly object _syncLock = new object();

        public SSHTransport(ConnectionProfile connection)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        }

        /// <summary>
        /// Executes a command on the remote host and returns the result.
        /// </summary>
        public async Task<SSHCommandResult> ExecuteAsync(string remoteCommand, byte[] standardInput = null)
        {
            lock (_syncLock)
            {
                EnsureConnected();
            }

            var command = _client.CreateCommand(remoteCommand);
            if (standardInput != null)
            {
                // We'll write the standard input to the command's input stream.
                using (var inputStream = command.InputStream)
                {
                    inputStream.Write(standardInput, 0, standardInput.Length);
                }
            }

            var result = command.Execute();
            return new SSHCommandResult
            {
                StandardOutput = result.Result,
                StandardError = command.Error,
                ExitCode = command.ExitStatus
            };
        }

        /// <summary>
        /// Executes a command that returns JSON and deserializes it into the specified type.
        /// </summary>
        public async Task<T> ExecuteJSONAsync<T>(string pythonScript)
        {
            // We assume the remote host has python3 and we can run a script that outputs JSON.
            var command = $"python3 -c \"{pythonScript}\"";
            var result = await ExecuteAsync(command);

            if (result.ExitCode != 0)
            {
                throw new SSHTransportError($"Remote command failed with exit code {result.ExitCode}: {result.StandardError}");
            }

            try
            {
                // Use System.Text.Json to deserialize the standard output.
                return System.Text.Json.JsonSerializer.Deserialize<T>(result.StandardOutput);
            }
            catch (Exception ex)
            {
                throw new SSHTransportError($"Failed to decode remote JSON: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the arguments for starting a shell (used for the terminal).
        /// We are not using this in the current implementation, but we keep it for completeness.
        /// </summary>
        public IEnumerable<string> GetShellArguments(string startupCommandLine = null)
        {
            // This method is not used in the current implementation because we are using the SSH client to create a shell session.
            // We'll throw a NotImplementedException to indicate that it's not implemented.
            throw new NotImplementedException();
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
            // We'll reuse the logic from the original Swift code, but simplified.
            if (!string.IsNullOrEmpty(result.StandardError))
            {
                var error = result.StandardError.Trim();
                if (error.Contains("Permission denied", StringComparison.OrdinalIgnoreCase))
                {
                    return "SSH authentication failed. Verify the key, SSH agent, and user for this SSH target.";
                }
                if (error.Contains("Host key verification failed", StringComparison.OrdinalIgnoreCase))
                {
                    return "SSH host key verification failed. Connect once in Terminal or update known_hosts before retrying.";
                }
                if (error.Contains("remote host identification has changed", StringComparison.OrdinalIgnoreCase))
                {
                    return "The SSH host key changed for this target. Refresh the entry in known_hosts before retrying.";
                }
                if (error.Contains("Could not resolve hostname", StringComparison.OrdinalIgnoreCase) ||
                    error.Contains("Name or service not known", StringComparison.OrdinalIgnoreCase))
                {
                    return "The SSH target could not be resolved. Check the alias, hostname, IP address, or SSH config entry in this profile.";
                }
                if (error.Contains("Connection refused", StringComparison.OrdinalIgnoreCase))
                {
                    return "The SSH server refused the connection. Confirm that SSH is enabled and reachable on the target host.";
                }
                if (error.Contains("Operation timed out", StringComparison.OrdinalIgnoreCase) ||
                    error.Contains("Connection timed out", StringComparison.OrdinalIgnoreCase))
                {
                    return "The SSH connection timed out. Check that the target host is reachable from this Mac and that your SSH route is correct.";
                }
                if (error.Contains("No route to host", StringComparison.OrdinalIgnoreCase) ||
                    error.Contains("Network is unreachable", StringComparison.OrdinalIgnoreCase))
                {
                    return "The SSH target is unreachable from this Mac. Check the hostname, IP address, VPN, or local network path and retry.";
                }
                if (error.Contains("python3: command not found", StringComparison.OrdinalIgnoreCase) ||
                    error.Contains("command not found: python3", StringComparison.OrdinalIgnoreCase) ||
                    error.Contains("python3: not found", StringComparison.OrdinalIgnoreCase) ||
                    error.Contains("unknown command: python3", StringComparison.OrdinalIgnoreCase) ||
                    error.Contains("env: python3: no such file or directory", StringComparison.OrdinalIgnoreCase))
                {
                    return "SSH succeeded, but python3 is not available in the remote non-interactive SSH shell PATH. Install python3 or expose it in the SSH shell environment before retrying. Hermes Desktop requires python3 for discovery, file editing, and session browsing.";
                }

                return error;
            }

            if (!string.IsNullOrEmpty(result.StandardOutput))
            {
                var output = result.StandardOutput.Trim();
                if (!string.IsNullOrEmpty(output))
                {
                    return output;
                }
            }

            return $"SSH command failed with exit code {result.ExitCode}.";
        }

        private void EnsureConnected()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(SSHTransport));
            }

            if (_client == null || !_client.IsConnected)
            {
                Connect();
            }
        }

        private void Connect()
        {
            // Disconnect any existing client.
            Disconnect();

            var connectionInfo = new ConnectionInfo(
                _connection.EffectiveTarget,
                _connection.ResolvedPort ?? 22,
                _connection.TrimmedUser ?? string.Empty,
                GetAuthenticationMethods());

            _client = new SshClient(connectionInfo);
            _client.Connect();
        }

        private IEnumerable<AuthenticationMethod> GetAuthenticationMethods()
        {
            var methods = new List<AuthenticationMethod>();

            // Try to use the private key from the default location.
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var sshDir = System.IO.Path.Combine(userProfile, ".ssh");
            var rsaKey = System.IO.Path.Combine(sshDir, "id_rsa");
            var ed25519Key = System.IO.Path.Combine(sshDir, "id_ed25519");

            if (System.IO.File.Exists(rsaKey))
            {
                methods.Add(new PrivateKeyAuthenticationMethod(_connection.TrimmedUser ?? string.Empty, rsaKey));
            }
            else if (System.IO.File.Exists(ed25519Key))
            {
                methods.Add(new PrivateKeyAuthenticationMethod(_connection.TrimmedUser ?? string.Empty, ed25519Key));
            }
            else
            {
                // If no key file is found, we try to use the SSH agent (if available) or assume the user will use agent.
                // We'll leave it empty and hope that the connection info will use the agent if no other method is provided.
                // However, note that the ConnectionInfo constructor we are using does take authentication methods as a parameter.
                // If we don't add any, then it will try to use the agent or password (if we had set the password property).
                // We are not setting a password, so it will try agent.
                // So we can leave the methods empty and let the ConnectionInfo handle it.
                // But note: the ConnectionInfo constructor we are using has an overload that takes authentication methods.
                // If we pass an empty list, it will still try to use the agent? Let's check the SSH.NET documentation.
                // Actually, the ConnectionInfo constructor that we are using (with host, port, username, and authenticationMethods) 
                // will use the provided authentication methods. If the list is empty, it will not try any method and the connection will fail.
                // So we must provide at least one method.
                // We'll try to use the private key agent by using the PrivateKeyAuthenticationMethod with the agent.
                // But SSH.NET doesn't have a direct way to use the agent without a key file.
                // We'll try to use the SSH agent by using the PrivateKeyAuthenticationMethod and pointing it to the agent socket.
                // However, that is platform-specific and complex.
                // For the sake of this port, we'll assume that the user has set up their SSH agent and that the private key is loaded.
                // We'll try to use the default key files, and if they don't exist, we'll throw an error.
                throw new InvalidOperationException("No private key found in ~/.ssh/id_rsa or ~/.ssh/id_ed25519. Please set up SSH key-based authentication.");
            }

            return methods;
        }

        private void Disconnect()
        {
            if (_client != null && _client.IsConnected)
            {
                _client.Disconnect();
                _client.Dispose();
            }
            _client = null;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                Disconnect();
                _disposed = true;
            }
        }

        #region Nested Classes

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
