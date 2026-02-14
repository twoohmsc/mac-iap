using CommunityToolkit.Mvvm.ComponentModel;
using Google.Solutions.Apis.Auth;
using Google.Solutions.Apis.Client;
using Google.Solutions.Apis.Compute;
using Google.Solutions.Apis.Locator;
using Google.Solutions.Iap;
using System.Collections.ObjectModel;

namespace IapDesktop.Application.Avalonia.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        [ObservableProperty]
        private string greeting = "Welcome to IAP Desktop for macOS";

        [ObservableProperty]
        private ProjectExplorerViewModel projectExplorer;

        [ObservableProperty]
        private ObservableCollection<ConnectionViewModel> connections = new ObservableCollection<ConnectionViewModel>();

        [ObservableProperty]
        private ConnectionViewModel? selectedConnection;

        private readonly IAuthorization authorization;
        private readonly UserAgent userAgent;
        private readonly IapClient iapClient;
        private readonly Google.Solutions.Platform.Security.Cryptography.IKeyStore keyStore;

        public MainViewModel(
            ComputeEngineClient computeClient,
            IAuthorization authorization,
            UserAgent userAgent,
            Google.Solutions.Platform.Security.Cryptography.IKeyStore keyStore)
        {
            this.authorization = authorization;
            this.userAgent = userAgent;
            this.keyStore = keyStore;

            this.iapClient = new IapClient(
                IapClient.CreateEndpoint(),
                authorization,
                userAgent);

            ProjectExplorer = new ProjectExplorerViewModel(this, computeClient, authorization, userAgent);
        }

        public void OpenConnection(InstanceLocator instance)
        {
            // Create a new connection tab
            var vm = new ConnectionViewModel(instance, iapClient, authorization, keyStore);
            Connections.Add(vm);
            SelectedConnection = vm;
            
            // Start connection
            // Fire and forget for now, but should be tracked
            _ = vm.ConnectAsync();
        }

        public MainViewModel()
        {
            // Design-time
            ProjectExplorer = new ProjectExplorerViewModel();
        }

        [ObservableProperty]
        private string output = "Output will appear here...";
    }
}
