using Google.Solutions.Apis.Auth;
using Google.Solutions.Apis.Auth.Gaia;
using Google.Solutions.Apis.Client;
using Google.Solutions.Apis.Compute;
using Google.Solutions.Apis.Locator;
using Google.Solutions.Common.Util;
using Google.Solutions.Iap;
using Google.Solutions.Iap.Net;
using Google.Solutions.Platform.Security.Cryptography;
using Google.Solutions.Ssh;
using Google.Solutions.Ssh.Native;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
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

            if (args.Length >= 4 && args[0] == "--test-ssh")
            {
                // Usage: --test-ssh <project> <zone> <instance>
                await TestSsh(args[1], args[2], args[3]);
                return;
            }

            if (args.Length == 0)
            {
                System.Console.WriteLine("Usage: IapDesktop.Console <project-id>");
                System.Console.WriteLine("       IapDesktop.Console --test-keychain");
                System.Console.WriteLine("       IapDesktop.Console --test-ssh <project-id> <zone> <instance-name>");
                return;
            }

            var projectId = args[0];
            // ... (rest of Main)
        }

        static async Task TestSsh(string projectId, string zone, string instanceName)
        {
            try
            {
                System.Console.WriteLine($"Testing SSH connection to {instanceName} in {zone} (Project: {projectId})...");

                if (long.TryParse(projectId, out _))
                {
                    System.Console.WriteLine("Error: Project ID must be a string (e.g., 'my-project-id'), not a numeric Project Number.");
                    return;
                }

                // 1. Setup Dependencies
                var credentialStorePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".iapdesktop.json");
                var store = new FileOidcOfflineCredentialStore(credentialStorePath);
                var enrollment = new DeviceEnrollment();
                var userAgent = new UserAgent("IapDesktop.Console", new Version(1, 0), Environment.OSVersion.ToString());
                var authClient = new GaiaOidcClient(
                    GaiaOidcClient.CreateEndpoint(),
                    enrollment,
                    store,
                    new OidcClientRegistration(
                        OidcIssuer.Gaia,
                        OAuthClient.ClientId,
                        OAuthClient.ClientSecret,
                        "/authorize/"),
                    userAgent);

                // 2. Authorize
                System.Console.WriteLine("Authorizing...");
                IOidcSession session;

                if (OAuthClient.ClientId == "YOUR_CLIENT_ID_HERE")
                {
                    System.Console.WriteLine("Using Application Default Credentials (gcloud)...");
                    var gcloudCredential = await GoogleCredential.GetApplicationDefaultAsync();
                    if (gcloudCredential.IsCreateScopedRequired)
                    {
                        gcloudCredential = gcloudCredential.CreateScoped(
                            "https://www.googleapis.com/auth/cloud-platform",
                            "https://www.googleapis.com/auth/compute",
                            "email",
                            "profile");
                    }
                    
                    string email = "gcloud-user@example.com";
                    try
                    {
                        var token = await gcloudCredential.UnderlyingCredential.GetAccessTokenForRequestAsync();
                        using (var client = new System.Net.Http.HttpClient())
                        {
                            var response = await client.GetAsync($"https://www.googleapis.com/oauth2/v3/tokeninfo?access_token={token}");
                            if (response.IsSuccessStatusCode)
                            {
                                var json = await response.Content.ReadAsStringAsync();
                                var emailPart = json.Split(new[] { "\"email\": \"" }, StringSplitOptions.None)[1];
                                email = emailPart.Split('"')[0];
                            }
                        }
                    }
                    catch { }

                    session = new GcloudOidcSession(gcloudCredential, email);
                }
                else
                {
                    session = await authClient.AuthorizeAsync(new LocalServerCodeReceiver(), CancellationToken.None);
                }

                //
                // 3. Prepare SSH Credential
                //
                var authorization = new AuthorizationAdapter(session, enrollment);
                var keyStore = new Google.Solutions.Platform.Security.Cryptography.KeychainKeyStore();

                var username = session.Username.Split('@')[0].ToLowerInvariant();
                var keyName = $"IAPDESKTOP_{username}_ssh";
                var keyType = new Google.Solutions.Platform.Security.Cryptography.KeyType(
                    System.Security.Cryptography.CngAlgorithm.Rsa, 
                    3072);

                System.Console.WriteLine($"Using Keychain credential: {keyName}");
                using (var credential = new Google.Solutions.Ssh.KeychainSshCredential(username, keyStore, keyName, keyType))
                {
                    System.Console.WriteLine($"--------------------------------------------------------------------------------");
                    System.Console.WriteLine($"Public Key: {credential.Signer.PublicKey}");
                    System.Console.WriteLine($"--------------------------------------------------------------------------------");
                    // 4. Create IAP Tunnel
                    var iapClient = new IapClient(IapClient.CreateEndpoint(), authorization, userAgent);
                    var instanceLocator = new InstanceLocator(projectId, zone, instanceName);
                    var target = iapClient.GetTarget(instanceLocator, 22, IapClient.DefaultNetworkInterface);
                    
                    var listener = new IapListener(target, new AllowAllPolicy(), null);
                    var cts = new CancellationTokenSource();
                    _ = listener.ListenAsync(cts.Token);
                    System.Console.WriteLine($"IAP Listener started on {listener.LocalEndpoint}");

                    try
                    {
                        // 5. Connect SSH
                        System.Console.WriteLine("Connecting SSH session...");
                        using (var libssh2Session = new Libssh2Session())
                        {
                            var connectedSession = libssh2Session.Connect(listener.LocalEndpoint);
                            var authenticatedSession = connectedSession.Authenticate(credential, new TerminalKeyboardHandler());
                            
                            System.Console.WriteLine("SSH Authenticated. Executing command...");
                            using (var channel = authenticatedSession.OpenExecChannel("echo 'Hello from Keychain Connection!'"))
                            {
                                channel.WaitForEndOfStream();
                                var buffer = new byte[1024];
                                var bytesRead = channel.Read(buffer);
                                var output = Encoding.UTF8.GetString(buffer, 0, (int)bytesRead);
                                System.Console.WriteLine($"Command Output: {output.Trim()}");
                            }
                        }
                    }
                    finally
                    {
                        cts.Cancel();
                    }
                }
                System.Console.WriteLine("SSH Test Complete: SUCCESS");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"SSH Test Failed: {ex.Message}");
                if (ex.InnerException != null) System.Console.WriteLine($"Inner: {ex.InnerException.Message}");
                System.Console.WriteLine(ex.StackTrace);
            }
        }

        private class AllowAllPolicy : IIapListenerPolicy
        {
            public bool IsClientAllowed(IPEndPoint remote) => true;
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
        public event EventHandler? Reauthorized;

        public AuthorizationAdapter(IOidcSession session, IDeviceEnrollment enrollment)
        {
            Session = session;
            DeviceEnrollment = enrollment;
        }
    }

    internal class TerminalKeyboardHandler : IKeyboardInteractiveHandler
    {
        public string? Prompt(string caption, string instruction, string prompt, bool echo)
        {
            System.Console.WriteLine($"[Auth Prompt] {caption}: {instruction}");
            System.Console.Write(prompt);
            return System.Console.ReadLine();
        }

        public IPasswordCredential PromptForCredentials(string username)
        {
            throw new NotImplementedException("Password authentication is not supported in this test tool.");
        }
    }
}
