using CommunityToolkit.Mvvm.ComponentModel;
using Google.Solutions.Apis.Locator;
using System.Collections.ObjectModel;

namespace IapDesktop.Application.Avalonia.ViewModels
{
    public abstract partial class ProjectExplorerNode : ObservableObject
    {
        [ObservableProperty]
        private string name;

        [ObservableProperty]
        private string icon;

        [ObservableProperty]
        private bool isExpanded;

        [ObservableProperty]
        private bool isSelected;

        public ObservableCollection<ProjectExplorerNode> Children { get; } = new ObservableCollection<ProjectExplorerNode>();

        protected ProjectExplorerNode(string name, string icon)
        {
            this.name = name;
            this.icon = icon;
        }
    }

    public class ProjectNode : ProjectExplorerNode
    {
        public string ProjectId { get; }

        public ProjectNode(string projectId) : base(projectId, "☁️")
        {
            ProjectId = projectId;
            IsExpanded = true;
        }
    }

    public class ZoneNode : ProjectExplorerNode
    {
        public string ProjectId { get; }
        public string ZoneId { get; }

        public ZoneNode(string projectId, string zoneId) : base(zoneId, "🏢")
        {
            ProjectId = projectId;
            ZoneId = zoneId;
            IsExpanded = true;
        }
    }

    public class InstanceNode : ProjectExplorerNode
    {
        public InstanceLocator Instance { get; }
        public string OperatingSystem { get; } // Linux/Windows

        public InstanceNode(InstanceLocator instance, string os = "Linux") : base(instance.Name, "💻")
        {
            Instance = instance;
            OperatingSystem = os;
        }
    }
}
