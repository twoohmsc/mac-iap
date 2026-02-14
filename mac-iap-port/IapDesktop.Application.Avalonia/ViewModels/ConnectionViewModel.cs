using CommunityToolkit.Mvvm.ComponentModel;
using Google.Solutions.Apis.Auth;
using Google.Solutions.Apis.Client;
using Google.Solutions.Apis.Compute;
using Google.Solutions.Apis.Locator;
using Google.Solutions.Iap;
using Google.Solutions.Common.Runtime;
using Google.Solutions.Ssh;
using Google.Solutions.Ssh.Native;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IapDesktop.Application.Avalonia.ViewModels
{
    public partial class ConnectionViewModel : ObservableObject, IDisposable
    {
        private readonly InstanceLocator instance;
        private readonly IapClient iapClient;
        private readonly IAuthorization authorization;
        private readonly Google.Solutions.Platform.Security.Cryptography.IKeyStore keyStore;
        
        [ObservableProperty]
        private string title;

        [ObservableProperty]
        private string statusText;

        [ObservableProperty]
        private bool isConnected;

        public event EventHandler<string>? OutputReceived;
        public event EventHandler<string>? ConnectionError;

        private Libssh2Session? session;
        private Libssh2ShellChannel? channel;

        public ConnectionViewModel(
            InstanceLocator instance,
            IapClient iapClient,
            IAuthorization authorization,
            Google.Solutions.Platform.Security.Cryptography.IKeyStore keyStore)
        {
            this.instance = instance;
            this.iapClient = iapClient;
            this.authorization = authorization;
            this.keyStore = keyStore;
            this.Title = instance.Name;
            this.StatusText = "Initializing...";
        }

        public async Task ConnectAsync()
        {
            await Task.Run(() =>
            {
                try
                {
                    StatusText = "Connecting via IAP...";
                    
                    // 1. Create IAP Listener (Local Port Forward)
                    var target = iapClient.GetTarget(
                        instance, 
                        22, 
                        IapClient.DefaultNetworkInterface);

                    var listener = new IapListener(
                        target, 
                        new AllowAllPolicy(), 
                        null); // Dynamic port

                    // Start listening
                    var cts = new CancellationTokenSource();
                    _ = listener.ListenAsync(cts.Token);
                    
                    StatusText = $"IAP tunnel listening on {listener.LocalEndpoint}";

                    // 2. Load Credential from Keychain
                    ISshCredential credential;
                    try
                    {
                        // Extract username from email (e.g., user@example.com -> user)
                        var email = authorization.Session.Username;
                        var username = email.Split('@')[0].ToLowerInvariant();
                        
                        // Use RSA 3072-bit key (standard for SSH)
                        var keyType = new Google.Solutions.Platform.Security.Cryptography.KeyType(
                            System.Security.Cryptography.CngAlgorithm.Rsa, 
                            3072);
                        
                        var keyName = $"IAPDESKTOP_{username}_ssh";

                        credential = new KeychainSshCredential(
                            username,
                            keyStore,
                            keyName,
                            keyType);
                        
                        StatusText += $"\nLoaded SSH key from Keychain for {username}";
                    }
                    catch (Exception ex)
                    {
                        StatusText += $"\nFailed to load Keychain key: {ex.Message}";
                        throw new Exception($"Failed to initialize SSH credential from Keychain: {ex.Message}", ex);
                    }

                    // 3. Establish SSH Session
                    StatusText += "\nEstablishing SSH session...";
                    
                    this.session = new Libssh2Session();
                    
                    // Connect to the local IAP endpoint
                    var connectedSession = this.session.Connect(listener.LocalEndpoint);
                    
                    // Authenticate
                    var authenticatedSession = connectedSession.Authenticate(
                        credential,
                        new GuiKeyboardInteractiveHandler());
                    
                    StatusText += "\nOpening shell channel...";
                    
                    // Open Shell Channel
                    this.channel = authenticatedSession.OpenShellChannel(
                        LIBSSH2_CHANNEL_EXTENDED_DATA.MERGE,
                        "xterm",
                        80,
                        24);
                        
                    StatusText = "Connected.";
                    IsConnected = true;

                    // 4. Start Output Loop
                    _ = ReadOutputAsync();
                }
                catch (Exception ex)
                {
                    StatusText = $"Error: {ex.Message}";
                    ConnectionError?.Invoke(this, ex.Message);
                    OnOutputReceived($"\nConnection failed: {ex.Message}\n");
                }
            });
        }

        private async Task ReadOutputAsync()
        {
            try
            {
                var buffer = new byte[1024];
                while (channel != null && IsConnected)
                {
                    // Wrap synchronous Read in Task.Run to avoid blocking UI
                    var read = await Task.Run(() => channel.Read(buffer));
                    if (read == 0) 
                    {
                        // No data available, wait a bit
                        await Task.Delay(10);
                        continue;
                    }
                    
                    var text = Encoding.UTF8.GetString(buffer, 0, (int)read);
                    OnOutputReceived(text);
                }
            }
            catch (Exception ex)
            {
                OnOutputReceived($"\nRead error: {ex.Message}\n");
            }
            finally
            {
                IsConnected = false;
                StatusText = "Disconnected.";
            }
        }

        public async Task SendInputAsync(string input)
        {
            if (!IsConnected || channel == null) return;

            try
            {
                var data = Encoding.UTF8.GetBytes(input);
                // Wrap synchronous Write in Task.Run
                await Task.Run(() => channel.Write(data));
            }
            catch (Exception ex)
            {
                OnOutputReceived($"\nWrite error: {ex.Message}\n");
            }
        }

        private void OnOutputReceived(string text)
        {
            OutputReceived?.Invoke(this, text);
        }

        public void Dispose()
        {
            session?.Dispose();
            channel?.Dispose();
        }

        private class AllowAllPolicy : IIapListenerPolicy
        {
            public bool IsClientAllowed(IPEndPoint remote) => true;
        }

        private class GuiKeyboardInteractiveHandler : IKeyboardInteractiveHandler
        {
            public string? Prompt(string caption, string instruction, string prompt, bool echo)
            {
                // For now, just log and return null or empty? 
                // In a real GUI, we would show a dialog.
                System.Diagnostics.Debug.WriteLine($"[SSH Prompt] {caption}: {instruction} ({prompt})");
                return null;
            }

            public IPasswordCredential PromptForCredentials(string username)
            {
                throw new NotImplementedException("Password authentication is not yet supported in the GUI.");
            }
        }
    }
}
