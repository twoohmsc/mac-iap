using Google.Apis.Auth.OAuth2;
using Google.Solutions.Apis.Auth;
using Google.Solutions.Common.Util;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace IapDesktop.Application.Avalonia
{
    internal class GcloudOidcSession : IOidcSession
    {
        private readonly GoogleCredential credential;
        private readonly string email;

        public event EventHandler? Terminated;

        public GcloudOidcSession(GoogleCredential credential, string email)
        {
            this.credential = credential.ExpectNotNull(nameof(credential));
            this.email = email;
        }

        public string Username => this.email;

        public ICredential ApiCredential => this.credential;

        public OidcOfflineCredential OfflineCredential => null!;

        public Uri CreateDomainSpecificServiceUri(Uri target)
        {
            return target;
        }

        public Task RevokeGrantAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public void Splice(IOidcSession newSession)
        {
            // No-op
        }

        public void Terminate()
        {
            Terminated?.Invoke(this, EventArgs.Empty);
        }
    }
}
