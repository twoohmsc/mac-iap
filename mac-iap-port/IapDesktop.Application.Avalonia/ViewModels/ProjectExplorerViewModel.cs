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
        private async Task AddProject(string? projectId)
        {
            if (string.IsNullOrWhiteSpace(projectId))
            {
                return;
            }

            if (RootNodes.Any(n => n is ProjectNode p && p.ProjectId == projectId))
            {
                return; // Already added
            }

            try
            {
                var projectNode = new ProjectNode(projectId);
                RootNodes.Add(projectNode);

                var instances = await computeClient.ListInstancesAsync(
                    new ProjectLocator(projectId),
                    CancellationToken.None);

                // Group by Zone
                var zoneGroups = instances.GroupBy(i => i.Zone);

                foreach (var zoneGroup in zoneGroups)
                {
                    var zoneId = zoneGroup.Key.Contains('/')
                       ? zoneGroup.Key.Substring(zoneGroup.Key.LastIndexOf('/') + 1)
                       : zoneGroup.Key;

                    var zoneNode = new ZoneNode(projectId, zoneId);
                    projectNode.Children.Add(zoneNode);

                    foreach (var instance in zoneGroup)
                    {
                        var instanceLocator = new InstanceLocator(projectId, zoneId, instance.Name);

                        // Improved OS Detection: Check licenses and guest-os-features
                        var isWindows = instance.Disks.Any(d => d.Licenses?.Any(l => l.Contains("windows-server", System.StringComparison.OrdinalIgnoreCase)) ?? false) ||
                                       instance.GuestAccelerators.Any(a => a.AcceleratorType.Contains("windows", System.StringComparison.OrdinalIgnoreCase)); // Fallback or extra check
                        
                        // Check common Windows imaging features if licenses are missing
                        if (!isWindows && instance.Disks.Any(d => d.Architecture?.Equals("X86_64", System.StringComparison.OrdinalIgnoreCase) ?? false))
                        {
                            // This is still a bit of a guess if licenses are stripped, but licenses are the primary source.
                        }

                        var os = isWindows ? "Windows" : "Linux";
                        var instanceNode = new InstanceNode(instanceLocator, os);
                        zoneNode.Children.Add(instanceNode);
                    }
                }
            }
            catch (System.Exception ex)
            {
                // Find or add an error node specifically for this project or global?
                // For now, just show a message or add a dummy node under the project
                // RootNodes.Add(new ProjectNode($"Error ({projectId}): {ex.Message}"));
            }
        }

        [RelayCommand]
        private void RemoveProject(ProjectNode? projectNode)
        {
            if (projectNode != null)
            {
                RootNodes.Remove(projectNode);
            }
        }

        [RelayCommand]
        private async Task LoadNodes(string projectId)
        {
             await AddProject(projectId);
        }

        private string? CurrentProjectId;
    }
}
