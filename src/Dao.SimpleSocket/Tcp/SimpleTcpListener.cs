using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Dao.SimpleSocket.Tcp
{
    public class SimpleTcpListener : IDisposable
    {
        TcpListener listener;
        bool isRunning;

        public Action<LogLevel, string> OnLogging { get; set; }
        public Func<TcpClient, Task> OnReceiving { get; set; }

        public void Start(int port)
        {
            if (this.listener != null)
                return;

            if (port <= 0)
                throw new ArgumentOutOfRangeException(nameof(port));

            this.listener = new TcpListener(IPAddress.Any, port);
            this.listener.Start();
            this.isRunning = true;
            _ = Receive();
            OnLogging?.Invoke(LogLevel.Information, $"Hosting TcpListener on port \"{port}\".");
        }

        async Task Receive()
        {
            while (this.isRunning && this.listener != null && OnReceiving != null)
            {
                try
                {
                    var client = await this.listener.AcceptTcpClientAsync().ConfigureAwait(false);
                    OnLogging?.Invoke(LogLevel.Information, $"Incoming TcpClient from \"{client.Client.RemoteEndPoint}\".");
                    _ = Receiving(client);
                }
                catch (Exception ex)
                {
                    OnLogging?.Invoke(LogLevel.Error, ex.ToString());
                }
            }
        }

        async Task Receiving(TcpClient client)
        {
            try
            {
                using (client)
                {
                    var onReceiving = OnReceiving;
                    if (onReceiving != null)
                        await onReceiving(client).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                OnLogging?.Invoke(LogLevel.Error, ex.ToString());
            }
        }

        public void Stop()
        {
            if (this.listener == null)
                return;

            this.isRunning = false;
            this.listener.Stop();
            this.listener = null;
        }

        public void Dispose()
        {
            Stop();
            OnReceiving = null;
            OnLogging = null;
        }
    }
}