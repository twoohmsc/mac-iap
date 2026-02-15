using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Google.Solutions.Apis.Auth;
using Google.Solutions.Apis.Client;
using Google.Solutions.Apis.Compute;
using Google.Solutions.Apis.Locator;
using Google.Solutions.Iap;
using Google.Solutions.Iap.Net;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace IapDesktop.Application.Avalonia.ViewModels
{
    public partial class ProjectExplorerViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<ProjectExplorerNode> rootNodes = new ObservableCollection<ProjectExplorerNode>();

        private readonly ComputeEngineClient computeClient;
        private readonly MainViewModel mainViewModel;
        private readonly IAuthorization authorization;
        private readonly UserAgent userAgent;
        
        [ObservableProperty]
        private ProjectExplorerNode? selectedNode;

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

        [RelayCommand]
        private async Task Connect()
        {
             if (SelectedNode is InstanceNode instanceNode)
             {
                 mainViewModel.OpenConnection(instanceNode.Instance);
             }
        }

        [RelayCommand]
        private async Task ConnectRdp()
        {
             if (SelectedNode is InstanceNode instanceNode)
             {
                 mainViewModel.OpenRdpSession(instanceNode.Instance);
             }
        }

        [RelayCommand]
        private async Task ConnectSftp()
        {
             if (SelectedNode is InstanceNode instanceNode)
             {
                 mainViewModel.OpenSftpSession(instanceNode.Instance);
             }
        }

        [RelayCommand]
        private async Task LoadNodes(string projectId)
        {
             CurrentProjectId = projectId;
             RootNodes.Clear();
             
             try
             {
                 // Create root Project Node
                 var projectNode = new ProjectNode(projectId);
                 RootNodes.Add(projectNode);

                 var instances = await computeClient.ListInstancesAsync(
                     new ProjectLocator(projectId), 
                     CancellationToken.None);
                 
                 // Group by Zone
                 var zoneGroups = instances.GroupBy(i => i.Zone);

                 foreach (var zoneGroup in zoneGroups)
                 {
                     // Extract zone name from full URL or partial URL
                     var zoneId = zoneGroup.Key.Contains('/') 
                        ? zoneGroup.Key.Substring(zoneGroup.Key.LastIndexOf('/') + 1)
                        : zoneGroup.Key;

                     var zoneNode = new ZoneNode(projectId, zoneId);
                     projectNode.Children.Add(zoneNode);

                     foreach (var instance in zoneGroup)
                     {
                         // Determine OS icon if possible (or default)
                         var instanceLocator = new InstanceLocator(projectId, zoneId, instance.Name);
                         
                         var isWindows = instance.Disks.Any(d => d.Licenses?.Any(l => l.Contains("windows-server", System.StringComparison.OrdinalIgnoreCase)) ?? false);
                         var os = isWindows ? "Windows" : "Linux";

                         var instanceNode = new InstanceNode(instanceLocator, os);
                         zoneNode.Children.Add(instanceNode);
                     }
                 }
             }
             catch (System.Exception ex)
             {
                 // Add error node
                 RootNodes.Clear();
                 RootNodes.Add(new ProjectNode($"Error: {ex.Message}"));
             }
        }

        private string? CurrentProjectId;
    }
}
