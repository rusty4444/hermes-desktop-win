using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using HermesDesktop.WinUI.Services;

namespace HermesDesktop.WinUI.Views
{
    public sealed partial class TerminalView : Page
    {
        private TerminalService _terminalService;
        private bool _isConnected = false;

        public TerminalView()
        {
            this.InitializeComponent();
        }

        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            var appState = AppState.Instance;
            var connection = appState.ActiveConnection;

            if (connection == null || string.IsNullOrWhiteSpace(connection.EffectiveTarget))
            {
                StatusText.Text = "No connection configured. Go to Settings.";
                return;
            }

            ConnectingSpinner.Visibility = Visibility.Visible;
            StatusText.Text = "Connecting...";
            ConnectButton.IsEnabled = false;

            try
            {
                _terminalService = new TerminalService(connection);
                await _terminalService.ConnectAsync();
                _isConnected = true;

                // Start reading output
                _ = ReadTerminalOutputAsync();

                CommandInput.IsEnabled = true;
                SendButton.IsEnabled = true;
                DisconnectButton.IsEnabled = true;
                ConnectButton.IsEnabled = false;
                StatusText.Text = $"Connected to {connection.EffectiveTarget}";

                TerminalOutput.Text = "Connected to " + connection.EffectiveTarget + "\n";
            }
            catch (Exception ex)
            {
                StatusText.Text = "Connection failed: " + ex.Message;
                _terminalService?.Dispose();
                _terminalService = null;
            }
            finally
            {
                ConnectingSpinner.Visibility = Visibility.Collapsed;
                ConnectButton.IsEnabled = true;
            }
        }

        private void DisconnectButton_Click(object sender, RoutedEventArgs e)
        {
            _terminalService?.Dispose();
            _terminalService = null;
            _isConnected = false;

            CommandInput.IsEnabled = false;
            SendButton.IsEnabled = false;
            DisconnectButton.IsEnabled = false;
            ConnectButton.IsEnabled = true;
            StatusText.Text = "Disconnected";

            TerminalOutput.Text += "\n--- Disconnected ---\n";
            TerminalOutputScroller.ScrollToVerticalOffset(TerminalOutputScroller.ScrollableHeight);
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            SendCommand();
        }

        private void CommandInput_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                SendCommand();
                e.Handled = true;
            }
        }

        private async void SendCommand()
        {
            if (!_isConnected || _terminalService == null) return;

            var command = CommandInput.Text;
            if (string.IsNullOrWhiteSpace(command)) return;

            TerminalOutput.Text += "$ " + command + "\n";
            CommandInput.Text = string.Empty;

            try
            {
                await _terminalService.WriteAsync(command + "\n");
            }
            catch (Exception ex)
            {
                TerminalOutput.Text += "Error: " + ex.Message + "\n";
            }
        }

        private async Task ReadTerminalOutputAsync()
        {
            while (_isConnected && _terminalService != null)
            {
                try
                {
                    var output = await _terminalService.ReadAsync();
                    if (!string.IsNullOrEmpty(output))
                    {
                        await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                        {
                            TerminalOutput.Text += output;
                            // Scroll to end
                            TerminalOutputScroller.ScrollToVerticalOffset(
                                TerminalOutputScroller.ScrollableHeight);
                        });
                    }
                }
                catch (Exception)
                {
                    // Connection lost
                    if (_isConnected)
                    {
                        _isConnected = false;
                        await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                        {
                            StatusText.Text = "Connection lost";
                            CommandInput.IsEnabled = false;
                            SendButton.IsEnabled = false;
                            DisconnectButton.IsEnabled = false;
                            ConnectButton.IsEnabled = true;
                            TerminalOutput.Text += "\n--- Connection lost ---\n";
                        });
                    }
                    break;
                }

                await Task.Delay(100);
            }
        }
    }
}
