using Google.Solutions.Common.Util;
using Google.Solutions.Ssh;
using Google.Solutions.Ssh.Native;
using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace IapDesktop.Application.Avalonia
{
    public class SingleCommandSshWorker
    {
        private readonly IPEndPoint endpoint;
        private readonly ISshCredential credential;
        private readonly string command;

        public SingleCommandSshWorker(
            IPEndPoint endpoint,
            ISshCredential credential,
            string command)
        {
            this.endpoint = endpoint.ExpectNotNull(nameof(endpoint));
            this.credential = credential.ExpectNotNull(nameof(credential));
            this.command = command.ExpectNotNull(nameof(command));
        }

        public Task<string> ExecuteAsync()
        {
            return Task.Run(() =>
            {
                using (var session = new Libssh2Session())
                {
                    // session.SetTraceHandler(...) if needed
                    session.Timeout = TimeSpan.FromSeconds(10);

                    using (var connectedSession = session.Connect(this.endpoint))
                    using (var authenticatedSession = connectedSession.Authenticate(
                        this.credential,
                        new KeyboardInteractiveHandler()))
                    using (var channel = authenticatedSession.OpenExecChannel(this.command))
                    {
                        channel.WaitForEndOfStream();
                        
                        // Read output
                        var buffer = new byte[1024];
                        var output = new StringBuilder();
                        while (true)
                        {
                            var bytesRead = channel.Read(buffer);
                            if (bytesRead == 0) break;
                            output.Append(Encoding.UTF8.GetString(buffer, 0, (int)bytesRead));
                        }

                        return output.ToString();
                    }
                }
            });
        }

        private class KeyboardInteractiveHandler : IKeyboardInteractiveHandler
        {
            public string[] Interact(string name, string instruction, string[] prompts, bool[] echoes)
            {
                return new string[prompts.Length];
            }

            public string Prompt(string name, string instruction, string prompt, bool echo)
            {
                return string.Empty;
            }

            public IPasswordCredential PromptForCredentials(string prompt)
            {
                return null!;
            }
        }
    }
}
