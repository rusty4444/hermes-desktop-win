using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Renci.SshNet;
using Renci.SshNet.Common;
using HermesDesktop.WinUI.Models;

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

                var authMethods = GetAuthenticationMethods().ToArray();
                var host = _connection.EffectiveTarget;
                var port = _connection.ResolvedPort ?? 22;
                var connectionInfo = new ConnectionInfo(host, port, _connection.TrimmedUser ?? string.Empty,
                    ProxyTypes.None, string.Empty, 0, string.Empty, string.Empty, authMethods);

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
                    // Resize not available in SSH.NET 2024.1 ShellStream
                    // Column/row sizes tracked for future API update
                    // _shellStream.Resize((int)columns, (int)rows);
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
            var username = _connection.TrimmedUser ?? string.Empty;

            // 1. Try explicit password if set
            if (!string.IsNullOrEmpty(_connection.Password))
            {
                methods.Add(new PasswordAuthenticationMethod(username, _connection.Password));
            }

            // 2. Try private key files
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var sshDir = System.IO.Path.Combine(userProfile, ".ssh");

            var keyPaths = new[]
            {
                System.IO.Path.Combine(sshDir, "id_ed25519"),
                System.IO.Path.Combine(sshDir, "id_rsa"),
                System.IO.Path.Combine(sshDir, "id_ecdsa"),
            };

            foreach (var keyPath in keyPaths)
            {
                if (System.IO.File.Exists(keyPath))
                {
                    try
                    {
                        var privateKeyFile = new PrivateKeyFile(keyPath);
                        methods.Add(new PrivateKeyAuthenticationMethod(username, privateKeyFile));
                    }
                    catch
                    {
                        // Skip unreadable keys
                    }
                }
            }

            if (methods.Count == 0)
            {
                throw new InvalidOperationException(
                    "No authentication methods available. " +
                    "Ensure an SSH key exists or set a password in the connection profile.");
            }

            return methods;
        }
    }
}
