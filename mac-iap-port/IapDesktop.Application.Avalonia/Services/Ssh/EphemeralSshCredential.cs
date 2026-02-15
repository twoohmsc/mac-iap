using Google.Solutions.Common.Runtime;
using Google.Solutions.Ssh.Cryptography;
using Google.Solutions.Ssh;
using System.Security.Cryptography;

namespace IapDesktop.Application.Avalonia.Services.Ssh
{
    /// <summary>
    /// SSH credential that uses an ephemeral, in-memory key.
    /// Use this as a fallback when Keychain is unavailable.
    /// </summary>
    public class EphemeralSshCredential : DisposableBase, IAsymmetricKeyCredential
    {
        private readonly RSA key;

        public IAsymmetricKeySigner Signer { get; }
        public string Username { get; }

        public EphemeralSshCredential(string username)
        {
            this.Username = username;

            // Generate an ephemeral 3072-bit RSA key
            this.key = RSA.Create(3072);

            // Create signer from the key
            this.Signer = AsymmetricKeySigner.Create(this.key, true);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.key?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
