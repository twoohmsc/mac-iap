using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Google.Solutions.Apis.Auth;
using Google.Solutions.Apis.Locator;
using Google.Solutions.Iap;
using Google.Solutions.Iap.Protocol;
using Google.Solutions.Ssh;
using Google.Solutions.Ssh.Native;
using IapDesktop.Application.Avalonia.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace IapDesktop.Application.Avalonia.ViewModels
{
    public partial class SftpBrowserViewModel : ObservableObject, ISessionViewModel
    {
        private readonly InstanceLocator instance;
        private readonly IIapClient iapClient;
        private readonly IAuthorization authorization;
        private readonly Google.Solutions.Platform.Security.Cryptography.IKeyStore keyStore;
        private readonly IapDesktop.Application.Avalonia.Services.Ssh.ISshKeyService sshKeyService;
        private readonly IFilePickerService filePickerService;
        
        private IapListener? listener;
        private SshConnection? connection;
        private SftpChannel? sftpChannel;
        private CancellationTokenSource? cts;

        [ObservableProperty]
        private string title;

        [ObservableProperty]
        private string statusText;

        [ObservableProperty]
        private bool isConnected;

        [ObservableProperty]
        private string currentPath;

        [ObservableProperty]
        private ObservableCollection<SftpFileInfo> files = new ObservableCollection<SftpFileInfo>();

        [ObservableProperty]
        private SftpFileInfo? selectedFile;

        public SftpBrowserViewModel(
            InstanceLocator instance,
            IIapClient iapClient,
            IAuthorization authorization,
            Google.Solutions.Platform.Security.Cryptography.IKeyStore keyStore,
            IapDesktop.Application.Avalonia.Services.Ssh.ISshKeyService sshKeyService,
            IapDesktop.Application.Avalonia.Services.IFilePickerService filePickerService)
        {
            this.instance = instance;
            this.iapClient = iapClient;
            this.authorization = authorization;
            this.keyStore = keyStore;
            this.sshKeyService = sshKeyService;
            this.filePickerService = filePickerService;
            this.Title = $"SFTP: {instance.Name}";
            this.StatusText = "Initializing...";
            this.CurrentPath = "/";
        }

        public async Task ConnectAsync()
        {
            try
            {
                StatusText = "Connecting...";

                // 1. IAP Tunnel
                var target = iapClient.GetTarget(
                    instance,
                    22,
                    IapClient.DefaultNetworkInterface);

                this.listener = new IapListener(
                    target,
                    new AllowAllPolicy(),
                    null);
                
                this.cts = new CancellationTokenSource();
                _ = this.listener.ListenAsync(this.cts.Token);

                // 2. SSH Credential
                var email = authorization.Session.Username;
                var username = email.Split('@')[0].ToLowerInvariant();
                
                IAsymmetricKeyCredential credential;
                try
                {
                    var keyType = new Google.Solutions.Platform.Security.Cryptography.KeyType(
                        System.Security.Cryptography.CngAlgorithm.Rsa, 3072);
                    var keyName = $"IAPDESKTOP_{username}_ssh";
                    
                    credential = new KeychainSshCredential(
                        username,
                        keyStore,
                        keyName,
                        keyType);
                }
                catch (Exception ex)
                {
                    StatusText = $"Keychain failed: {ex.Message}. Using ephemeral key.";
                    credential = new IapDesktop.Application.Avalonia.Services.Ssh.EphemeralSshCredential(username);
                }

                // 3. Authorize Key
                StatusText = "Authorizing SSH key...";
                await this.sshKeyService.AuthorizeKeyAsync(
                    instance,
                    credential.Signer,
                    TimeSpan.FromMinutes(10),
                    CancellationToken.None);

                // 4. Connect SSH
                this.connection = new SshConnection(
                    this.listener.LocalEndpoint,
                    credential,
                    new GuiKeyboardInteractiveHandler());
                
                await this.connection.ConnectAsync();

                // 4. Open SFTP
                this.sftpChannel = await this.connection.OpenFileSystemAsync();
                
                StatusText = "Connected.";
                IsConnected = true;

                // 5. List Initial Directory (Home)
                await NavigateToAsync($"/home/{username}");
            }
            catch (Exception ex)
            {
                StatusText = $"Error: {ex.Message}";
                IsConnected = false;
            }
        }

        public async Task NavigateToAsync(string path)
        {
            if (this.sftpChannel == null) return;

            try
            {
                StatusText = $"Listing {path}...";
                var items = await this.sftpChannel.ListFilesAsync(path);
                
                Files.Clear();
                // Filter out . and .. if needed, or keep them. 
                // Usually .. is useful.
                foreach (var item in items.OrderByDescending(i => i.IsDirectory).ThenBy(i => i.Name))
                {
                    // Skip .
                    if (item.Name == ".") continue;
                    Files.Add(item);
                }

                CurrentPath = path;
                StatusText = $"Showing {path}";
            }
            catch (Exception ex)
            {
                StatusText = $"Failed to list {path}: {ex.Message}";
            }
        }

        [RelayCommand]
        public async Task NavigateUp()
        {
            if (string.IsNullOrEmpty(CurrentPath) || CurrentPath == "/") return;
            
            var parent = System.IO.Path.GetDirectoryName(CurrentPath);
            // Fix for root
            if (string.IsNullOrEmpty(parent)) parent = "/";
            // GetDirectoryName on Linux path mostly works but returns backslashes on Windows?
            // Since this runs on Mac/Linux (net8), Path.GetDirectoryName might use system separator.
            // Better to use string manipulation ensuring forward slashes.
            
            var lastSlash = CurrentPath.LastIndexOf('/');
            if (lastSlash <= 0) parent = "/";
            else parent = CurrentPath.Substring(0, lastSlash);

            await NavigateToAsync(parent);
        }

        [RelayCommand]
        public async Task OpenSelected()
        {
            if (SelectedFile == null) return;

            if (SelectedFile.Value.IsDirectory)
            {
                var newPath = CurrentPath == "/" 
                    ? $"/{SelectedFile.Value.Name}" 
                    : $"{CurrentPath}/{SelectedFile.Value.Name}";
                await NavigateToAsync(newPath);
            }
            else
            {
                // File selected - maybe prompt to download?
            }
        }

        [RelayCommand]
        public async Task Download()
        {
            if (SelectedFile == null || SelectedFile.Value.IsDirectory || this.sftpChannel == null || this.connection == null) return;

            try
            {
                var remotePath = CurrentPath == "/" 
                    ? $"/{SelectedFile.Value.Name}" 
                    : $"{CurrentPath}/{SelectedFile.Value.Name}";

                var downloadsPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
                var localPath = System.IO.Path.Combine(downloadsPath, SelectedFile.Value.Name);

                StatusText = $"Downloading {SelectedFile.Value.Name}...";

                using (var remoteStream = await this.sftpChannel.CreateFileAsync(
                    remotePath, 
                    System.IO.FileMode.Open, 
                    System.IO.FileAccess.Read, 
                    FilePermissions.OwnerRead))
                using (var localStream = System.IO.File.Create(localPath))
                {
                    await remoteStream.CopyToAsync(localStream);
                }

                StatusText = $"Downloaded to {localPath}";
            }
            catch (Exception ex)
            {
                StatusText = $"Download failed: {ex.Message}";
            }
        }

        [RelayCommand]
        public async Task Upload()
        {
             if (this.sftpChannel == null || this.connection == null) return;

             try
             {
                 var files = await this.filePickerService.OpenFilesAsync(allowMultiple: true);
                 if (files.Count == 0) return;

                 foreach (var localPath in files)
                 {
                     var fileName = System.IO.Path.GetFileName(localPath);
                     var remotePath = CurrentPath == "/" 
                         ? $"/{fileName}" 
                         : $"{CurrentPath}/{fileName}";
                    
                     StatusText = $"Uploading {fileName}...";

                     using (var localStream = System.IO.File.OpenRead(localPath))
                     using (var remoteStream = await this.sftpChannel.CreateFileAsync(
                         remotePath,
                         System.IO.FileMode.Create,
                         System.IO.FileAccess.Write,
                         FilePermissions.OwnerRead | FilePermissions.OwnerWrite))
                     {
                         await localStream.CopyToAsync(remoteStream);
                     }
                 }

                 StatusText = "Upload completed.";
                 await NavigateToAsync(CurrentPath);
             }
             catch (Exception ex)
             {
                 StatusText = $"Upload failed: {ex.Message}";
             }
        }

        [RelayCommand]
        public async Task Refresh()
        {
            await NavigateToAsync(CurrentPath);
        }

        public void Dispose()
        {
            this.cts?.Cancel();
            this.sftpChannel?.Dispose();
            this.connection?.Dispose();
            this.listener = null;
        }

        private class AllowAllPolicy : IIapListenerPolicy
        {
            public bool IsClientAllowed(IPEndPoint remote) => true;
        }
    }
}
