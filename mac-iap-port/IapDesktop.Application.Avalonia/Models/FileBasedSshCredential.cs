using Google.Solutions.Common.Runtime;
using Google.Solutions.Ssh;
using Google.Solutions.Ssh.Cryptography;
using System;
using System.IO;
using System.Security.Cryptography;

namespace IapDesktop.Application.Avalonia.Models
{
    public class FileBasedSshCredential : DisposableBase, IAsymmetricKeyCredential
    {
        private readonly RSA rsa;
        public IAsymmetricKeySigner Signer { get; }
        public string Username { get; }

        public FileBasedSshCredential(string username, string keyFilePath)
        {
            this.Username = username;

            var pem = File.ReadAllText(keyFilePath);
            this.rsa = RSA.Create();
            
            // Try import as private key
            try
            {
                this.rsa.ImportFromPem(pem);
            }
            catch (Exception)
            {
                // Retry? Or throw. 
                // .NET 8 ImportFromPem handles various formats (Pkcs8, Pkcs1, etc)
                throw;
            }

            this.Signer = new RsaSigner(this.rsa, false); 
            // ownsKey=false in Signer because we own it here and dispose it
        }

        protected override void Dispose(bool disposing)
        {
            this.rsa.Dispose();
            base.Dispose(disposing);
        }

        public new void Dispose()
        {
            base.Dispose();
        }
    }
}
