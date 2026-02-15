using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Ssh;
using Google.Solutions.Ssh.Native;
using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace IapDesktop.Application.Avalonia.Services
{
    public class TerminalSshWorker : SshWorkerThread
    {
        public event EventHandler<string>? ReceiveData;
        public event EventHandler<Exception>? Error;
        public event EventHandler? Connected;

        private readonly StringBuilder sendQueue = new StringBuilder();
        private readonly object sendLock = new object();

        private Libssh2ShellChannel? shellChannel;

        public TerminalSshWorker(
            IPEndPoint endpoint,
            ISshCredential credential,
            IKeyboardInteractiveHandler keyboardHandler)
            : base(endpoint, credential, keyboardHandler)
        {
        }

        public void Connect()
        {
            StartConnection();
        }

        public Task ResizeTerminalAsync(ushort width, ushort height)
        {
            return Task.Run(() =>
            {
                try
                {
                    this.shellChannel?.ResizePseudoTerminal(width, height);
                }
                catch (Exception e)
                {
                    SshTraceSource.Log.TraceError(e);
                }
            });
        }

        public Task SendAsync(string data)
        {
            lock (this.sendLock)
            {
                this.sendQueue.Append(data);
            }

            // Notify worker thread that we have data to send
            NotifyReadyToSend(true);
            return Task.CompletedTask;
        }

        protected override void OnConnected()
        {
            // Trigger OnReadyToSend to ensure the channel gets opened immediately
            NotifyReadyToSend(true);
            this.Connected?.Invoke(this, EventArgs.Empty);
        }

        protected override void OnReadyToReceive(Libssh2AuthenticatedSession session)
        {
            EnsureChannelOpen(session);

            if (this.shellChannel == null) return;

            try
            {
                // Read from channel (non-blocking)
                var buffer = new byte[4096];
                var bytesRead = this.shellChannel.Read(buffer);

                if (bytesRead > 0)
                {
                    var text = Encoding.UTF8.GetString(buffer, 0, (int)bytesRead);
                    this.ReceiveData?.Invoke(this, text);
                }
            }
            catch (Libssh2Exception e) when (e.ErrorCode == LIBSSH2_ERROR.EAGAIN)
            {
                // No data available, expected
            }
            catch (Exception e)
            {
                this.Error?.Invoke(this, e);
            }
        }

        protected override void OnReadyToSend(Libssh2AuthenticatedSession session)
        {
            EnsureChannelOpen(session);

            if (this.shellChannel == null) return;

            string? dataToSend = null;
            lock (this.sendLock)
            {
                if (this.sendQueue.Length > 0)
                {
                    dataToSend = this.sendQueue.ToString();
                    this.sendQueue.Clear();
                }
            }

            if (!string.IsNullOrEmpty(dataToSend))
            {
                try
                {
                    var bytes = Encoding.UTF8.GetBytes(dataToSend);
                    this.shellChannel.Write(bytes);
                }
                catch (Libssh2Exception e) when (e.ErrorCode == LIBSSH2_ERROR.EAGAIN)
                {
                    // Can't write right now, put it back in queue
                    lock (this.sendLock)
                    {
                        this.sendQueue.Insert(0, dataToSend);
                    }
                }
                catch (Exception e)
                {
                    this.Error?.Invoke(this, e);
                }
            }
            else
            {
                // Nothing to send, reset event
                NotifyReadyToSend(false);
            }
        }

        private void EnsureChannelOpen(Libssh2AuthenticatedSession session)
        {
            if (this.shellChannel == null)
            {
                // We are in non-blocking mode, but OpenShellChannel works best in blocking mode.
                // Switch to blocking mode temporarily.
                try 
                {
                    using (session.Session.AsBlocking())
                    {
                        this.shellChannel = session.OpenShellChannel(
                            LIBSSH2_CHANNEL_EXTENDED_DATA.MERGE,
                            "xterm",
                            80, 24);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"DEBUG: TerminalSshWorker OpenShellChannel Failed: {ex}");
                    throw;
                }
            }
        }

        protected override void OnReceiveError(Exception exception)
        {
            this.Error?.Invoke(this, exception);
        }

        protected override void OnSendError(Exception exception)
        {
            this.Error?.Invoke(this, exception);
        }

        protected override void OnConnectionError(Exception exception)
        {
             this.Error?.Invoke(this, exception);
        }

        protected override void OnBeforeCloseSession()
        {
            this.shellChannel?.Dispose();
            this.shellChannel = null;
        }
    }
}
