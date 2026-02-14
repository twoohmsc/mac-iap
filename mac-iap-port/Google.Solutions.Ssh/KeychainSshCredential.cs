using Google.Solutions.Common.Runtime;
using Google.Solutions.Platform.Security.Cryptography;
using Google.Solutions.Ssh.Cryptography;
using System;
using System.Security.Cryptography;

namespace Google.Solutions.Ssh
{
    /// <summary>
    /// SSH credential that uses macOS Keychain for key storage.
    /// </summary>
    public class KeychainSshCredential : DisposableBase, IAsymmetricKeyCredential
    {
        private readonly AsymmetricAlgorithm key;
        public IAsymmetricKeySigner Signer { get; }
        public string Username { get; }

        public KeychainSshCredential(
            string username,
            IKeyStore keyStore,
            string keyName,
            KeyType keyType)
        {
            this.Username = username;

            // Open or create key in Keychain
            this.key = keyStore.OpenKey(
                IntPtr.Zero,  // No window handle needed for macOS
                keyName,
                keyType,
                CngKeyUsages.Signing,
                false);  // Don't force recreate

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

        public new void Dispose()
        {
            base.Dispose();
        }
    }
}
