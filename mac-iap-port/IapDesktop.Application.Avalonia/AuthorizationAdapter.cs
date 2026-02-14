using Google.Solutions.Apis.Auth;
using System;

namespace IapDesktop.Application.Avalonia
{
    /// <summary>
    /// Adapter to convert IOidcSession to IAuthorization.
    /// </summary>
    internal class AuthorizationAdapter : IAuthorization
    {
        private readonly IOidcSession session;

        public AuthorizationAdapter(IOidcSession session)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
        }

        public IOidcSession Session => this.session;

        public IDeviceEnrollment DeviceEnrollment => null!;

        public event EventHandler? Reauthorized;

        public string? DeviceEnrollmentToken => this.session.ApiCredential.GetAccessTokenForRequestAsync().Result;

        public string Email => this.session.Username;
    }
}
