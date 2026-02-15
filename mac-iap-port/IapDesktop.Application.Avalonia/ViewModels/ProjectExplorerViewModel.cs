using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Google.Solutions.Apis.Auth;
using Google.Solutions.Apis.Client;
using Google.Solutions.Apis.Compute;
using Google.Solutions.Apis.Locator;
using Google.Solutions.Iap;
using Google.Solutions.Iap.Net;
using Avalonia.Threading;
using System;
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
        private readonly IapDesktop.Application.Avalonia.Services.IProjectRepository projectRepository;
        
        [ObservableProperty]
        private ProjectExplorerNode? selectedNode;

        public ProjectExplorerViewModel(
            MainViewModel mainViewModel, 
            ComputeEngineClient computeClient,
            IAuthorization authorization, 
            UserAgent userAgent,
            IapDesktop.Application.Avalonia.Services.IProjectRepository projectRepository)
        {
            this.mainViewModel = mainViewModel;
            this.computeClient = computeClient;
            this.authorization = authorization;
            this.userAgent = userAgent;
            this.projectRepository = projectRepository;

            // Load persisted projects
            foreach (var projectId in projectRepository.ListProjects())
            {
                _ = AddProject(projectId);
            }
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
                Console.WriteLine("DEBUG: AddProject called with null/empty projectId");
                return;
            }

            Console.WriteLine($"DEBUG: AddProject/Refresh for '{projectId}'");

            var projectNode = RootNodes.OfType<ProjectNode>().FirstOrDefault(n => n.ProjectId == projectId);
            if (projectNode == null)
            {
                projectNode = new ProjectNode(projectId);
                RootNodes.Add(projectNode);
                projectRepository.AddProject(projectId);
                Console.WriteLine($"DEBUG: Added new ProjectNode for '{projectId}'");
            }
            else
            {
                Console.WriteLine($"DEBUG: Refreshing existing ProjectNode for '{projectId}'");
                projectNode.Children.Clear();
            }

            try
            {
                Console.WriteLine($"DEBUG: Listing instances for '{projectId}'...");
                var instances = await computeClient.ListInstancesAsync(
                    new ProjectLocator(projectId),
                    CancellationToken.None);
                
                Console.WriteLine($"DEBUG: Found {instances?.Count() ?? 0} instances in project '{projectId}'");

                if (instances == null || !instances.Any())
                {
                    Console.WriteLine("DEBUG: No instances found or instances list is null.");
                    return;
                }

                // Group by Zone
                var zoneGroups = instances.GroupBy(i => i.Zone);

                foreach (var zoneGroup in zoneGroups)
                {
                    var zoneId = zoneGroup.Key.Contains('/')
                       ? zoneGroup.Key.Substring(zoneGroup.Key.LastIndexOf('/') + 1)
                       : zoneGroup.Key;

                    Console.WriteLine($"DEBUG: Adding ZoneNode '{zoneId}'");
                    var zoneNode = new ZoneNode(projectId, zoneId);
                    
                    await Dispatcher.UIThread.InvokeAsync(() => {
                        projectNode.Children.Add(zoneNode);
                    });

                    int count = 0;
                    foreach (var instance in zoneGroup)
                    {
                        var instanceLocator = new InstanceLocator(projectId, zoneId, instance.Name);

                        // Ensure collections are not null before calling Any()
                        var isWindows = (instance.Disks?.Any(d => d.Licenses?.Any(l => l.Contains("windows-server", System.StringComparison.OrdinalIgnoreCase)) ?? false) ?? false) ||
                                       (instance.GuestAccelerators?.Any(a => a.AcceleratorType.Contains("windows", System.StringComparison.OrdinalIgnoreCase)) ?? false);
                        
                        var os = isWindows ? "Windows" : "Linux";
                        var instanceNode = new InstanceNode(instanceLocator, os);
                        
                        await Dispatcher.UIThread.InvokeAsync(() => {
                            zoneNode.Children.Add(instanceNode);
                        });
                        count++;
                    }
                    Console.WriteLine($"DEBUG: Added {count} instances to Zone '{zoneId}'");
                }
            }
            catch (System.Exception ex)
            {
                Console.WriteLine($"DEBUG: AddProject failed for '{projectId}': {ex}");
                await Dispatcher.UIThread.InvokeAsync(() => {
                    var errorNode = new ProjectNode($"Error: {ex.Message}");
                    projectNode.Children.Add(errorNode);
                });
            }
        }

        [RelayCommand]
        private void RemoveProject(ProjectNode? projectNode)
        {
            if (projectNode != null)
            {
                Console.WriteLine($"DEBUG: Removing ProjectNode for '{projectNode.ProjectId}'");
                RootNodes.Remove(projectNode);
                projectRepository.RemoveProject(projectNode.ProjectId);
            }
        }

        [RelayCommand]
        private async Task LoadNodes(string projectId)
        {
             Console.WriteLine($"DEBUG: LoadNodes triggered for '{projectId}'");
             await AddProject(projectId);
        }
    }
}
