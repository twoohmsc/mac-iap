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
    public partial class ConnectionViewModel : ObservableObject, ISessionViewModel
    {
        private readonly InstanceLocator instance;
        private readonly IapClient iapClient;
        private readonly IAuthorization authorization;
        private readonly Google.Solutions.Platform.Security.Cryptography.IKeyStore keyStore;
        private readonly IapDesktop.Application.Avalonia.Services.Ssh.ISshKeyService sshKeyService;
        
        [ObservableProperty]
        private string title;

        [ObservableProperty]
        private string statusText;

        [ObservableProperty]
        private bool isConnected;

        public event EventHandler<string>? OutputReceived;
        public event EventHandler<string>? ConnectionError;

        private IapDesktop.Application.Avalonia.Services.TerminalSshWorker? worker;

        public ConnectionViewModel(
            InstanceLocator instance,
            IapClient iapClient,
            IAuthorization authorization,
            Google.Solutions.Platform.Security.Cryptography.IKeyStore keyStore,
            IapDesktop.Application.Avalonia.Services.Ssh.ISshKeyService sshKeyService)
        {
            this.instance = instance;
            this.iapClient = iapClient;
            this.authorization = authorization;
            this.keyStore = keyStore;
            this.sshKeyService = sshKeyService;
            this.Title = instance.Name;
            this.StatusText = "Initializing...";
        }

        public async Task ConnectAsync()
        {
            await Task.Run(async () =>
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
                    KeychainSshCredential credential;
                    try
                    {
                        var email = authorization.Session.Username;
                        var username = email.Split('@')[0].ToLowerInvariant();
                        
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

                    // 3. Authorize Key (Push to Metadata/OS Login)
                    StatusText += "\nAuthorizing SSH key...";
                    try
                    {
                        await sshKeyService.AuthorizeKeyAsync(
                            instance,
                            credential.Signer,
                            TimeSpan.FromMinutes(10),
                            CancellationToken.None);
                         StatusText += "\nKey authorized successfully.";
                    }
                    catch (Exception ex)
                    {
                         StatusText += $"\nKey authorization failed: {ex.Message}";
                         throw new Exception($"Failed to authorize SSH key: {ex.Message}", ex);
                    }

                    // 4. Establish SSH Session using TerminalSshWorker
                    StatusText += "\nConnecting to SSH...";
                    
                    this.worker = new IapDesktop.Application.Avalonia.Services.TerminalSshWorker(
                        listener.LocalEndpoint,
                        credential,
                        new IapDesktop.Application.Avalonia.Services.GuiKeyboardInteractiveHandler());
                    
                    this.worker.ReceiveData += (s, data) => OnOutputReceived(data);
                    this.worker.Error += (s, ex) => 
                    {
                        OnOutputReceived($"\nSSH Error: {ex.Message}\n");
                        ConnectionError?.Invoke(this, ex.Message);
                        StatusText = $"Error: {ex.Message}";
                        IsConnected = false;
                    };
                    this.worker.Connected += (s, e) =>
                    {
                         StatusText = "Connected via SSH Worker.";
                         IsConnected = true;
                    };

                    this.worker.Connect();
                }
                catch (Exception ex)
                {
                    StatusText = $"Error: {ex.Message}";
                    ConnectionError?.Invoke(this, ex.Message);
                    OnOutputReceived($"\nConnection failed: {ex.Message}\n");
                }
            });
        }

        // ReadOutputAsync is no longer needed as the worker handles it via events.

        public async Task SendInputAsync(string input)
        {
            if (!IsConnected || worker == null) return;
            await worker.SendAsync(input);
        }

        public async Task ResizeTerminalAsync(ushort columns, ushort rows)
        {
            if (!IsConnected || worker == null) return;
            await worker.ResizeTerminalAsync(columns, rows);
        }

        private void OnOutputReceived(string text)
        {
            OutputReceived?.Invoke(this, text);
        }

        public void Dispose()
        {
            worker?.Dispose();
        }

        private class AllowAllPolicy : IIapListenerPolicy
        {
            public bool IsClientAllowed(IPEndPoint remote) => true;
        }


    }
}
