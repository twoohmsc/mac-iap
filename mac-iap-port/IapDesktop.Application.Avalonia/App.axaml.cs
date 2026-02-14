using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Google.Apis.Auth.OAuth2;
using Google.Solutions.Apis.Auth;
using Google.Solutions.Apis.Auth.Gaia;
using Google.Solutions.Apis.Client;
using Google.Solutions.Apis.Compute;
using System;
using System.Threading;

namespace IapDesktop.Application.Avalonia
{
    public partial class App : global::Avalonia.Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override async void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                //
                // Initialize Auth and Client.
                //
                var enrollment = new DeviceEnrollment();
                var userAgent = new UserAgent(
                    "IapDesktop.Application.Avalonia", 
                    new Version(1, 0),
                    Environment.OSVersion.ToString());
                
                var credentialStorePath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    ".iapdesktop.json");
                var store = new FileOidcOfflineCredentialStore(credentialStorePath);

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

                // Initialize KeyStore for SSH key management
                var keyStore = new Google.Solutions.Platform.Security.Cryptography.KeychainKeyStore();

                //
                // Try to authorize silently or interactively.
                //
                try 
                {
                    IOidcSession session;

                    if (OAuthClient.ClientId == "YOUR_CLIENT_ID_HERE")
                    {
                        //
                        // Fallback: Use Application Default Credentials (gcloud)
                        //
                        Console.WriteLine("Using Application Default Credentials (gcloud)...");
                        var gcloudCredential = await GoogleCredential.GetApplicationDefaultAsync();
                        if (gcloudCredential.IsCreateScopedRequired)
                        {
                            gcloudCredential = gcloudCredential.CreateScoped(
                                "https://www.googleapis.com/auth/cloud-platform",
                                "https://www.googleapis.com/auth/compute",
                                "email",
                                "profile");
                        }
                        
                        // Try to get the email address from the token info
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
                                    // Simple parse to avoid dependency
                                    var emailPart = json.Split(new[] { "\"email\": \"" }, StringSplitOptions.None)[1];
                                    email = emailPart.Split('"')[0];
                                }
                            }
                        }
                        catch { /* Best effort */ }

                        session = new GcloudOidcSession(gcloudCredential, email);
                    }
                    else
                    {
                        //
                        // Standard OAuth Flow
                        //
                        session = await authClient.AuthorizeAsync(
                            new LocalServerCodeReceiver(),
                            CancellationToken.None);
                    }
                    
                    var authorization = new AuthorizationAdapter(session);
                    
                    var computeClient = new ComputeEngineClient(
                        ComputeEngineClient.CreateEndpoint(),
                        authorization,
                        userAgent);

                    desktop.MainWindow = new MainWindow
                    {
                        DataContext = new ViewModels.MainViewModel(computeClient, authorization, userAgent, keyStore)
                    };
                }
                catch (Exception e)
                {
                    // Show error in a simple window or console
                    Console.WriteLine(e);
                }
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
