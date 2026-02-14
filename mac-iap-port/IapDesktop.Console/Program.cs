using Google.Solutions.Apis.Auth;
using Google.Solutions.Apis.Auth.Gaia;
using Google.Solutions.Apis.Client;
using Google.Solutions.Apis.Compute;
using Google.Solutions.Apis.Locator;
using Google.Solutions.Common.Util;
using Google.Solutions.Iap;
using Google.Solutions.Iap.Net;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;

namespace IapDesktop.Console
{
    class Program
    {
        static async Task Main(string[] args)
        {
            System.Console.WriteLine("IAP Desktop CLI - Verification Tool");

            if (args.Length > 0 && args[0] == "--test-keychain")
            {
                TestKeychain();
                return;
            }

            if (args.Length == 0)
            {
                System.Console.WriteLine("Usage: IapDesktop.Console <project-id>");
                System.Console.WriteLine("       IapDesktop.Console --test-keychain");
                return;
            }

            var projectId = args[0];

            try
            {
                var credentialStorePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    ".iapdesktop.json");
                
                var store = new FileOidcOfflineCredentialStore(credentialStorePath);
                var enrollment = new DeviceEnrollment();
                var userAgent = new UserAgent(
                    "IapDesktop.Console", 
                    new Version(1, 0),
                    Environment.OSVersion.ToString());

                //
                // 1. Authorize
                //

                var authClient = new GaiaOidcClient(
                    GaiaOidcClient.CreateEndpoint(),
                    enrollment,
                    store,
                    new OidcClientRegistration(
                        OidcIssuer.Gaia,
                        "74657-iap-desktop-client-id.apps.googleusercontent.com",
                        "secret",
                        "/authorize/"),
                    userAgent);

                System.Console.WriteLine("Authorizing...");
                var session = await authClient.AuthorizeAsync(
                    new LocalServerCodeReceiver(),
                    CancellationToken.None);
                
                var authorization = new AuthorizationAdapter(session, enrollment);

                System.Console.WriteLine($"Successfully authorized as: {authorization.Session.Username}");

                //
                // 2. List Instances
                //
                System.Console.WriteLine($"Listing instances in project {projectId}...");
                var computeClient = new ComputeEngineClient(
                    ComputeEngineClient.CreateEndpoint(),
                    authorization,
                    userAgent);

                var instances = await computeClient.ListInstancesAsync(
                    new ProjectLocator(projectId),
                    CancellationToken.None);

                var instance = instances.FirstOrDefault();
                if (instance == null)
                {
                    System.Console.WriteLine("No instances found.");
                    return;
                }

                System.Console.WriteLine($"Found instance: {instance.Name} ({instance.Zone})");

                //
                // 3. Probe IAP Connection
                //
                System.Console.WriteLine("Probing IAP connection to SSH port...");
                var iapClient = new IapClient(
                    IapClient.CreateEndpoint(),
                    authorization,
                    userAgent);

                var instanceLocator = new InstanceLocator(projectId, instance.Zone.Substring(instance.Zone.LastIndexOf('/') + 1), instance.Name);
                var target = iapClient.GetTarget(
                    instanceLocator,
                    22,
                    IapClient.DefaultNetworkInterface);

                await target.ProbeAsync(TimeSpan.FromSeconds(10));

                System.Console.WriteLine("Successfully probed connection! IAP and TCP relay are working.");
            }
            catch (Exception e)
            {
                System.Console.WriteLine($"Error: {e.Message}");
                if (e.InnerException != null)
                {
                    System.Console.WriteLine($"Inner Error: {e.InnerException.Message}");
                }
                
                // Print stack trace for debugging
                System.Console.WriteLine(e.StackTrace);
            }
        }

        static void TestKeychain()
        {
            var keychain = new Google.Solutions.Platform.Security.Keychain();
            var service = "IapDesktopTest";
            var account = "TestUser";
            var password = System.Text.Encoding.UTF8.GetBytes("SecretPassword123!");
            
            System.Console.WriteLine($"Adding password for {service}/{account}...");
            try
            {
                keychain.DeleteGenericPassword(service, account); // Cleanup
            } 
            catch {}

            keychain.AddGenericPassword(service, account, password);
            System.Console.WriteLine("Added.");
            
            System.Console.WriteLine("Retrieving password...");
            var retrieved = keychain.FindGenericPassword(service, account);
            if (retrieved != null)
            {
                System.Console.WriteLine($"Retrieved: {System.Text.Encoding.UTF8.GetString(retrieved)}");
            }
            else
            {
                System.Console.WriteLine("Failed to retrieve password.");
            }
            
            System.Console.WriteLine("Deleting password...");
            keychain.DeleteGenericPassword(service, account);
            System.Console.WriteLine("Deleted.");
        }
    }

    public class DeviceEnrollment : IDeviceEnrollment
    {
        public DeviceEnrollmentState State => DeviceEnrollmentState.NotEnrolled;
        public System.Security.Cryptography.X509Certificates.X509Certificate2? Certificate => null;
        public string? DeviceId => null;

        public Task RefreshAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    public class AuthorizationAdapter : IAuthorization
    {
        public IOidcSession Session { get; }
        public IDeviceEnrollment DeviceEnrollment { get; }
        public event EventHandler Reauthorized;

        public AuthorizationAdapter(IOidcSession session, IDeviceEnrollment enrollment)
        {
            Session = session;
            DeviceEnrollment = enrollment;
        }
    }
}
