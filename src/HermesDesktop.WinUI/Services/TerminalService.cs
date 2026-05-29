using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Renci.SshNet;
using Renci.SshNet.Common;

namespace HermesDesktop.WinUI.Services
{
    /// <summary>
    /// Service for managing an embedded SSH terminal session.
    /// </summary>
    public class TerminalService : IDisposable
    {
        private readonly ConnectionProfile _connection;
        private SshClient _client;
        private ShellStream _shellStream;
        private bool _disposed;
        private readonly object _syncLock = new object();

        public TerminalService(ConnectionProfile connection)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        }

        /// <summary>
        /// Connects to the remote host and starts a shell session.
        /// </summary>
        public async Task ConnectAsync()
        {
            lock (_syncLock)
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(nameof(TerminalService));
                }

                Disconnect(); // Ensure clean state

                var connectionInfo = new ConnectionInfo(
                    _connection.EffectiveTarget,
                    _connection.ResolvedPort ?? 22,
                    _connection.TrimmedUser ?? string.Empty,
                    GetAuthenticationMethods());

                _client = new SshClient(connectionInfo);
                _client.Connect();

                // Start a shell stream
                _shellStream = _client.CreateShellStream("xterm", 80, 24, 800, 600, 1024);
            }
        }

        /// <summary>
        /// Sends data to the terminal.
        /// </summary>
        public async Task WriteAsync(string data)
        {
            lock (_syncLock)
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(nameof(TerminalService));
                }

                if (_shellStream == null || !_shellStream.CanWrite)
                {
                    throw new InvalidOperationException("Terminal is not connected.");
                }

                _shellStream.Write(data);
                _shellStream.Flush();
            }
        }

        /// <summary>
        /// Reads available data from the terminal (non-blocking).
        /// </summary>
        public async Task<string> ReadAsync()
        {
            lock (_syncLock)
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(nameof(TerminalService));
                }

                if (_shellStream == null || !_shellStream.CanRead)
                {
                    return string.Empty;
                }

                // We'll read what's available without blocking
                var data = string.Empty;
                while (_shellStream.DataAvailable)
                {
                    var buffer = new byte[1024];
                    var bytesRead = _shellStream.Read(buffer, 0, buffer.Length);
                    data += System.Text.Encoding.UTF8.GetString(buffer, 0, bytesRead);
                }

                return data;
            }
        }

        /// <summary>
        /// Checks if there is data available to read.
        /// </summary>
        public bool DataAvailable => 
            !_disposed && 
            _shellStream != null && 
            _shellStream.DataAvailable;

        /// <summary>
        /// Resizes the terminal.
        /// </summary>
        public void ResizeTerminal(uint columns, uint rows)
        {
            lock (_syncLock)
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(nameof(TerminalService));
                }

                if (_shellStream != null)
                {
                    _shellStream.Resize((int)columns, (int)rows);
                }
            }
        }

        private void Disconnect()
        {
            if (_shellStream != null)
            {
                _shellStream.Close();
                _shellStream.Dispose();
                _shellStream = null;
            }

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
                throw new InvalidOperationException("No private key found in ~/.ssh/id_rsa or ~/.ssh/id_ed25519. Please set up SSH key-based authentication.");
            }

            return methods;
        }
    }
}
