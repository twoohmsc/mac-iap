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
        private ObservableCollection<ISessionViewModel> connections = new ObservableCollection<ISessionViewModel>();

        [ObservableProperty]
        private ISessionViewModel? selectedConnection;

        private readonly IAuthorization authorization;
        private readonly UserAgent userAgent;
        private readonly IapClient iapClient;
        private readonly Google.Solutions.Platform.Security.Cryptography.IKeyStore keyStore;
        private readonly IapDesktop.Application.Avalonia.Services.IRdpConnectionService rdpService;
        private readonly IapDesktop.Application.Avalonia.Services.Ssh.ISshKeyService sshKeyService;
        private readonly IapDesktop.Application.Avalonia.Services.IFilePickerService filePickerService;

        public MainViewModel(
            ComputeEngineClient computeClient,
            IAuthorization authorization,
            UserAgent userAgent,
            Google.Solutions.Platform.Security.Cryptography.IKeyStore keyStore,
            IapDesktop.Application.Avalonia.Services.Ssh.ISshKeyService sshKeyService,
            IapDesktop.Application.Avalonia.Services.IFilePickerService filePickerService)
        {
            this.authorization = authorization;
            this.userAgent = userAgent;
            this.keyStore = keyStore;
            this.sshKeyService = sshKeyService;
            this.filePickerService = filePickerService;

            this.iapClient = new IapClient(
                IapClient.CreateEndpoint(),
                authorization,
                userAgent);
            
            this.rdpService = new IapDesktop.Application.Avalonia.Services.RdpConnectionService();

            ProjectExplorer = new ProjectExplorerViewModel(this, computeClient, authorization, userAgent);
        }

        public void OpenConnection(InstanceLocator instance)
        {
            // Create a new connection tab
            var vm = new ConnectionViewModel(instance, iapClient, authorization, keyStore, sshKeyService);
            Connections.Add(vm);
            SelectedConnection = vm;
            
            // Start connection
            _ = vm.ConnectAsync();
        }

        public void OpenRdpSession(InstanceLocator instance)
        {
            var vm = new RdpSessionViewModel(instance, iapClient, rdpService, authorization);
            Connections.Add(vm);
            SelectedConnection = vm;
            _ = vm.ConnectAsync();
        }

        public void OpenSftpSession(InstanceLocator instance)
        {
            var vm = new SftpBrowserViewModel(instance, iapClient, authorization, keyStore, sshKeyService, filePickerService);
            Connections.Add(vm);
            SelectedConnection = vm;
            _ = vm.ConnectAsync();
        }

        public MainViewModel()
        {
            // Design-time
            ProjectExplorer = new ProjectExplorerViewModel();
            this.rdpService = new IapDesktop.Application.Avalonia.Services.RdpConnectionService();
            this.sshKeyService = new IapDesktop.Application.Avalonia.Services.Ssh.SshKeyService(null!, null!, null!);
        }

        [ObservableProperty]
        private string output = "Output will appear here...";
    }
}
