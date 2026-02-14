using Google.Apis.CloudOSLogin.v1.Data;
using Google.Solutions.Apis.Compute;
using Google.Solutions.Apis.Locator;
using Google.Solutions.Common.Util;
using Google.Solutions.Ssh.Cryptography;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace IapDesktop.Application.Avalonia.Services.Ssh
{
    public enum OsLoginSystemType
    {
        Linux
    }

    public class OsLoginProfile
    {
        private readonly IOsLoginClient client;

        public OsLoginProfile(IOsLoginClient client)
        {
            this.client = client;
        }

        public async Task ImportSshPublicKeyAsync(
            ProjectLocator project,
            string userEmail,
            IAsymmetricKeySigner key,
            TimeSpan validity,
            CancellationToken token)
        {
            // Convert key to OpenSSH format
            var publicKey = key.PublicKey.ToString(PublicKey.Format.OpenSsh);

            // Import the key. This will create the POSIX account if needed (implicit in API).
            // We use the simpler method from OsLoginClient.
            await this.client.ImportSshPublicKeyAsync(
                project,
                publicKey,
                validity,
                token).ConfigureAwait(false);
        }
    }
}
