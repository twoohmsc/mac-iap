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
        private readonly IDeviceEnrollment enrollment;

        public AuthorizationAdapter(IOidcSession session, IDeviceEnrollment enrollment)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            this.enrollment = enrollment ?? throw new ArgumentNullException(nameof(enrollment));
        }

        public IOidcSession Session => this.session;

        public IDeviceEnrollment DeviceEnrollment => this.enrollment;

        public event EventHandler? Reauthorized;

        public string? DeviceEnrollmentToken => this.session.ApiCredential.GetAccessTokenForRequestAsync().Result;

        public string Email => this.session.Username;
    }
}
