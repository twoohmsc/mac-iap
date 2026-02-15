using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace IapDesktop.Application.Avalonia.Services
{
    public class ProjectRepository : IProjectRepository
    {
        private readonly string filePath;
        private readonly HashSet<string> projects;

        public ProjectRepository(string filePath)
        {
            this.filePath = filePath;
            this.projects = Load();
        }

        public IEnumerable<string> ListProjects()
        {
            lock (projects)
            {
                return projects.ToList();
            }
        }

        public void AddProject(string projectId)
        {
            if (string.IsNullOrWhiteSpace(projectId)) return;

            lock (projects)
            {
                if (projects.Add(projectId.Trim()))
                {
                    Save();
                }
            }
        }

        public void RemoveProject(string projectId)
        {
            if (string.IsNullOrWhiteSpace(projectId)) return;

            lock (projects)
            {
                if (projects.Remove(projectId.Trim()))
                {
                    Save();
                }
            }
        }

        private HashSet<string> Load()
        {
            try
            {
                if (File.Exists(filePath))
                {
                    var json = File.ReadAllText(filePath);
                    var list = JsonSerializer.Deserialize<List<string>>(json);
                    return list != null ? new HashSet<string>(list, StringComparer.OrdinalIgnoreCase) : new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DEBUG: Failed to load projects from {filePath}: {ex.Message}");
            }
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        private void Save()
        {
            try
            {
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonSerializer.Serialize(projects.ToList());
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DEBUG: Failed to save projects to {filePath}: {ex.Message}");
            }
        }
    }
}
