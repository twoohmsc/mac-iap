using Google.Solutions.Apis.Auth;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace IapDesktop.Application.Avalonia
{
    public class DeviceEnrollment : IDeviceEnrollment
    {
        public DeviceEnrollmentState State => DeviceEnrollmentState.NotEnrolled;
        public X509Certificate2? Certificate => null;
        public string? DeviceId => null;

        public Task RefreshAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
