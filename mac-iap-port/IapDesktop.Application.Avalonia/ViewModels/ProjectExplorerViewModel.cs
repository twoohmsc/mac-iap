using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Google.Solutions.Apis.Auth;
using Google.Solutions.Apis.Client;
using Google.Solutions.Apis.Compute;
using Google.Solutions.Apis.Locator;
using Google.Solutions.Iap;
using Google.Solutions.Iap.Net;
using System.Collections.ObjectModel;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace IapDesktop.Application.Avalonia.ViewModels
{
    public partial class ProjectExplorerViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<string> nodes = new ObservableCollection<string>();

        private readonly ComputeEngineClient computeClient;
        private readonly MainViewModel mainViewModel;
        private readonly IAuthorization authorization;
        private readonly UserAgent userAgent;

        public ProjectExplorerViewModel(
            MainViewModel mainViewModel, 
            ComputeEngineClient computeClient,
            IAuthorization authorization, 
            UserAgent userAgent)
        {
            this.mainViewModel = mainViewModel;
            this.computeClient = computeClient;
            this.authorization = authorization;
            this.userAgent = userAgent;
        }

        public ProjectExplorerViewModel() 
        { 
             // Design-time constructor
        }

        [ObservableProperty]
        private string? selectedNode;

        [RelayCommand]
        private async Task Connect()
        {
             if (SelectedNode == null || CurrentProjectId == null) return;
            
             var parts = SelectedNode.Split(new[] { '(', ')' }, System.StringSplitOptions.RemoveEmptyEntries);
             if (parts.Length < 2) return;

             var instanceName = parts[0].Trim();
             var zone = parts[1].Trim();
             
             var locator = new InstanceLocator(CurrentProjectId, zone, instanceName);
             mainViewModel.OpenConnection(locator);
        }

        [RelayCommand]
        private async Task LoadNodes(string projectId)
        {
             CurrentProjectId = projectId;
             Nodes.Clear();
             try
             {
                 var instances = await computeClient.ListInstancesAsync(
                     new ProjectLocator(projectId), 
                     CancellationToken.None);
                 
                 foreach (var instance in instances)
                 {
                     Nodes.Add($"{instance.Name} ({instance.Zone.Substring(instance.Zone.LastIndexOf('/') + 1)})");
                 }
             }
             catch (System.Exception ex)
             {
                 Nodes.Add($"Error: {ex.Message}");
             }
        }

        private string? CurrentProjectId;
    }
}
