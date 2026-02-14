using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace IapDesktop.Application.Avalonia.Services
{
    public interface IRdpConnectionService
    {
        Task LaunchRdpAsync(IPEndPoint endpoint, string username, string instanceName);
    }

    public class RdpConnectionService : IRdpConnectionService
    {
        public async Task LaunchRdpAsync(IPEndPoint endpoint, string username, string instanceName)
        {
            var rdpContent = GenerateRdpFileContent(endpoint, username);
            var tempFile = Path.Combine(Path.GetTempPath(), $"{instanceName}.rdp");
            await File.WriteAllTextAsync(tempFile, rdpContent);

            // Open with default handler (Microsoft Remote Desktop)
            Process.Start(new ProcessStartInfo
            {
                FileName = "open",
                Arguments = tempFile,
                UseShellExecute = true
            });
        }

        private string GenerateRdpFileContent(IPEndPoint endpoint, string username)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"full address:s:{endpoint.Address}:{endpoint.Port}");
            sb.AppendLine($"username:s:{username}");
            sb.AppendLine("prompt for credentials:i:1");
            sb.AppendLine("administrative session:i:1");
            sb.AppendLine("screen mode id:i:2"); // Full screen
            sb.AppendLine("use multimon:i:1");
            return sb.ToString();
        }
    }
}
