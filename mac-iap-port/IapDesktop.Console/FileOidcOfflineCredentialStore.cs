using Google.Solutions.Apis.Auth;
using Newtonsoft.Json;
using System;
using System.IO;

namespace IapDesktop.Console
{
    public class FileOidcOfflineCredentialStore : IOidcOfflineCredentialStore
    {
        private readonly string filePath;

        public FileOidcOfflineCredentialStore(string filePath)
        {
            this.filePath = filePath;
        }

        public void Clear()
        {
            if (File.Exists(this.filePath))
            {
                File.Delete(this.filePath);
            }
        }

        public bool TryRead(out OidcOfflineCredential? credential)
        {
            if (File.Exists(this.filePath))
            {
                try
                {
                    var json = File.ReadAllText(this.filePath);
                    var data = JsonConvert.DeserializeObject<CredentialData>(json);
                    if (data != null)
                    {
                        credential = new OidcOfflineCredential(
                            OidcIssuer.Gaia,
                            data.Scope,
                            data.RefreshToken,
                            data.IdToken);
                        return true;
                    }
                }
                catch
                {
                    // Ignore errors
                }
            }

            credential = null;
            return false;
        }

        public void Write(OidcOfflineCredential credential)
        {
            var data = new CredentialData
            {
                Scope = credential.Scope,
                RefreshToken = credential.RefreshToken,
                IdToken = credential.IdToken
            };
            var json = JsonConvert.SerializeObject(data);
            File.WriteAllText(this.filePath, json);
        }

        private class CredentialData
        {
            public string Scope { get; set; } = string.Empty;
            public string RefreshToken { get; set; } = string.Empty;
            public string? IdToken { get; set; }
        }
    }
}
