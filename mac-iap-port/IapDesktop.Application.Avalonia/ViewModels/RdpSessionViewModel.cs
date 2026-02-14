using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Google.Solutions.Apis.Auth;
using Google.Solutions.Apis.Locator;
using Google.Solutions.Iap;
using Google.Solutions.Iap.Protocol;
using IapDesktop.Application.Avalonia.Services;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace IapDesktop.Application.Avalonia.ViewModels
{
    public partial class RdpSessionViewModel : ObservableObject, ISessionViewModel
    {
        private readonly InstanceLocator instance;
        private readonly IIapClient iapClient;
        private readonly IRdpConnectionService rdpService;
        private readonly IAuthorization authorization; // For username if needed
        private IapListener? listener;
        private CancellationTokenSource? cts;

        [ObservableProperty]
        private string title;

        [ObservableProperty]
        private string statusText;

        [ObservableProperty]
        private bool isConnected;

        public RdpSessionViewModel(
            InstanceLocator instance,
            IIapClient iapClient,
            IRdpConnectionService rdpService,
            IAuthorization authorization)
        {
            this.instance = instance;
            this.iapClient = iapClient;
            this.rdpService = rdpService;
            this.authorization = authorization;
            this.Title = $"RDP: {instance.Name}";
            this.StatusText = "Initializing...";
        }

        public async Task ConnectAsync()
        {
            try
            {
                StatusText = "Establishing IAP Tunnel...";
                
                // 1. Create IAP Listener
                var target = iapClient.GetTarget(
                    instance,
                    3389, // RDP port
                    IapClient.DefaultNetworkInterface);

                this.listener = new IapListener(
                    target,
                    new AllowAllPolicy(),
                    null);

                this.cts = new CancellationTokenSource();
                
                // Start listening
                _ = this.listener.ListenAsync(this.cts.Token);
                
                StatusText = $"Tunnel listening on {this.listener.LocalEndpoint}";
                IsConnected = true;

                // 2. Launch RDP
                StatusText += "\nLaunching Microsoft Remote Desktop...";
                // Use the user's email or "user" if not available, though RDP usually prompts or needs a specific Windows user.
                // For now pass the Google email as a hint.
                var username = authorization.Session?.Username ?? "user";
                await this.rdpService.LaunchRdpAsync(
                    this.listener.LocalEndpoint, 
                    username, 
                    this.instance.Name);
                
                StatusText += "\nRDP Client Launched.";
            }
            catch (Exception ex)
            {
                StatusText = $"Connection Failed: {ex.Message}";
                IsConnected = false;
            }
        }

        [RelayCommand]
        public void Disconnect()
        {
            Dispose();
            StatusText = "Disconnected.";
            IsConnected = false;
        }

        public void Dispose()
        {
            this.cts?.Cancel();
            this.cts?.Dispose();
            this.listener = null;
        }

        private class AllowAllPolicy : IIapListenerPolicy
        {
            public bool IsClientAllowed(IPEndPoint remote) => true;
        }
    }
}
