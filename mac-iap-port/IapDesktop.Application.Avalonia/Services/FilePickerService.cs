using Avalonia.Controls;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;

namespace IapDesktop.Application.Avalonia.Services
{
    public class FilePickerService : IFilePickerService
    {
        private readonly Window targetWindow;

        public FilePickerService(Window targetWindow)
        {
            this.targetWindow = targetWindow;
        }

        public async Task<IReadOnlyList<string>> OpenFilesAsync(bool allowMultiple = true)
        {
            var files = await targetWindow.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Open File(s)",
                AllowMultiple = allowMultiple
            });

            return files.Select(f => f.Path.LocalPath).ToList();
        }

        public async Task<string?> SaveFileAsync(string defaultName, string? defaultExtension = null)
        {
            var file = await targetWindow.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Save File",
                SuggestedFileName = defaultName,
                DefaultExtension = defaultExtension
            });

            return file?.Path.LocalPath;
        }
    }
}
