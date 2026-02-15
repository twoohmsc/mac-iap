using System.Collections.Generic;
using System.Threading.Tasks;

namespace IapDesktop.Application.Avalonia.Services
{
    public interface IFilePickerService
    {
        Task<IReadOnlyList<string>> OpenFilesAsync(bool allowMultiple = true);
        Task<string?> SaveFileAsync(string defaultName, string? defaultExtension = null);
    }
}
