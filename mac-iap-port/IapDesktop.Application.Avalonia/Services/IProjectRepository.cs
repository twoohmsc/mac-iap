using System.Collections.Generic;

namespace IapDesktop.Application.Avalonia.Services
{
    public interface IProjectRepository
    {
        IEnumerable<string> ListProjects();
        void AddProject(string projectId);
        void RemoveProject(string projectId);
    }
}
