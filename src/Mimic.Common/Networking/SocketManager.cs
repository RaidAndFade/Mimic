using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Mimic.Common
{
    public class SocketManager<THandler>
        where THandler : ISocketHandler, new()
    {
        private const int BufferSize = 6144;

        private readonly TcpListener _server;
        private ConcurrentBag<Task> _clientTasks;

        private Task _listenTask;
        private bool _listening;


        public SocketManager(
            string address = "0.0.0.0",
            ushort port = 3724)
        {
            var listenAddress = IPAddress.Parse(address);
            _server = new TcpListener(listenAddress, (int)port);
        }

        public async Task StartAsync()
        {
            if (_listening)
                await StopAsync().ConfigureAwait(false);

            _clientTasks = new ConcurrentBag<Task>();

            _listening = true;
            _server.Start();
            _listenTask = ListenAsync();

            Debug.WriteLine($"Listening on {_server.LocalEndpoint.ToString()}");
        }

        public async Task StopAsync()
        {
            _listening = false;
            await _listenTask
                .ConfigureAwait(false);
        }

        private async Task ListenAsync()
        {
            while (_listening)
            {
                var client = await _server.AcceptTcpClientAsync()
                    .ConfigureAwait(false);

                // TODO: this should be handled safer

                _clientTasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        await HandleClientAsync(client);
                    }
                    catch (IOException)
                    {
                        // client likely disconnected, or a net-split occured
                    }
                }));
            }

            try
            {
                await Task.WhenAll(_clientTasks)
                    .ConfigureAwait(false);
            }
            finally
            {
                _server.Stop();
            }
        }

        private async Task HandleClientAsync(TcpClient client)
        {
            var handler = new THandler();

            try
            {
                await handler.RunAsync(client)
                    .ConfigureAwait(false);
            }
            finally
            {
                (handler as IDisposable)?.Dispose();
                client.Close();
            }
        }
    }
}
