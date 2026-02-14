using Google.Solutions.Apis.Auth;
using Google.Solutions.Apis.Compute;
using Google.Solutions.Apis.Locator;
using Google.Solutions.Common.Util;
using Google.Solutions.Ssh.Cryptography;
using IapDesktop.Application.Avalonia.Services.Ssh.Metadata;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IapDesktop.Application.Avalonia.Services.Ssh
{
    public interface ISshKeyService
    {
        Task AuthorizeKeyAsync(
            InstanceLocator instance,
            IAsymmetricKeySigner key,
            TimeSpan validity,
            CancellationToken token);
    }

    public class SshKeyService : ISshKeyService
    {
        private readonly IAuthorization authorization;
        private readonly IComputeEngineClient computeClient;
        private readonly IOsLoginClient osLoginClient;

        public SshKeyService(
            IAuthorization authorization,
            IComputeEngineClient computeClient,
            IOsLoginClient osLoginClient)
        {
            this.authorization = authorization;
            this.computeClient = computeClient;
            this.osLoginClient = osLoginClient;
        }

        public async Task AuthorizeKeyAsync(
            InstanceLocator instance,
            IAsymmetricKeySigner key,
            TimeSpan validity,
            CancellationToken token)
        {
            // 1. Check Metadata for OS Login status
            var metadata = await InstanceMetadata.GetAsync(
                this.computeClient,
                instance,
                token).ConfigureAwait(false);

            if (metadata.IsOsLoginEnabled)
            {
                // OS Login Enforced
                var osLoginProfile = new OsLoginProfile(this.osLoginClient);
                await osLoginProfile.ImportSshPublicKeyAsync(
                    instance.Project,
                    this.authorization.Session.Username,
                    key,
                    validity,
                    token).ConfigureAwait(false);
            }
            else
            {
                // Metadata Authorization
                var username = SuggestUsername(this.authorization.Session.Username);
                
                // Create managed key
                var metadataKey = new ManagedMetadataAuthorizedPublicKey(
                    username,
                    key.PublicKey.Type,
                    Convert.ToBase64String(key.PublicKey.WireFormatValue),
                    new ManagedMetadataAuthorizedPublicKey.PublicKeyMetadata(
                        this.authorization.Session.Username,
                        DateTime.UtcNow.Add(validity)));

                await metadata.AddPublicKeyToMetadata(metadataKey, token).ConfigureAwait(false);
            }
        }

        private static string SuggestUsername(string email)
        {
            // Simple username derivation:
            // - lowercase
            // - replace @ and . with _
            // - truncate if too long (optional)
            
            var username = email.Split('@')[0].ToLowerInvariant();
            
            // Clean up invalid characters (simple approach)
            username = new string(username.Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray());

            return username;
        }
    }
}
