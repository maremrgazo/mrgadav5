#pragma warning disable CS8981 // suppress naming rule violation
#pragma warning disable CS8618 // suppress non-null value when exiting constructor
#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using Serilog;

public static partial class mrgada
{
    public class ServerCollector
    {
        protected readonly string _name;
        private readonly int _port;
        private volatile bool _started = false;

        private TcpListener tcpl_listener;

        private List<TcpClient> _clients = [];
        private object _clientsLock = new object();

        private Thread t_clientConnect;
        private bool b_clientConnect;
        private readonly int _clientConnectThreadTimeoutMilliseconds = 100;

        private Thread t_clientListener;
        private bool b_clientListener;
        private readonly int _clientListenerThreadTimeoutMilliseconds = 50;

        Thread t_clientDisconnect;
        bool b_clientDisconnect;
        private readonly int _clientDisconnectThreadTimeoutMilliseconds = 500;

        public ServerCollector
            (
            string name, 
            int port, 
            int clientConnectThreadTimeoutMilliseconds = 100, 
            int clientListenerThreadTimeoutMilliseconds = 100,
            int clientDisconnectThreadTimeoutMilliseconds = 500
            )
        {
            _name = name;
            _port = port;

            _clientConnectThreadTimeoutMilliseconds = clientConnectThreadTimeoutMilliseconds;
            _clientListenerThreadTimeoutMilliseconds = clientListenerThreadTimeoutMilliseconds;
            _clientDisconnectThreadTimeoutMilliseconds = clientDisconnectThreadTimeoutMilliseconds;

            mrgada.AddServerCollector(this);
        }

        public bool Started => _started;
        public bool Stopped => !_started;
        protected virtual void OnClientConnected(TcpClient client) { }
        protected virtual void OnClientDisconnected(TcpClient client) { } 
        protected virtual void OnRecieved(TcpClient client, byte[] data) { } // on server recieve message from client
        protected virtual void OnStart() { }
        protected virtual void OnStop() { }
        public void Broadcast(byte[] data)  // send message to all clients
        {
            if (_clients.Count <= 0 || Stopped) return;
            lock (_clientsLock)
            {
                foreach (TcpClient client in _clients)
                {
                    try
                    {
                        NetworkStream ns_stream = client.GetStream();
                        ns_stream.Write(data, 0, data.Length);
                    }
                    catch
                    {
                        Log.Information($"{_name} ServerCollector: Client disconnected while broadcasting started!");
                    }
                }
            }
        }

        public void Start()
        {
            tcpl_listener = new TcpListener(IPAddress.Parse(mrgada._serverIp), _port);
            tcpl_listener.Start();

            b_clientConnect = true;
            t_clientConnect = new Thread(ClientConnectThread);
            t_clientConnect.IsBackground = true;
            t_clientConnect.Start();

            b_clientDisconnect = true;
            t_clientDisconnect = new Thread(ClientDisconnectThread);
            t_clientDisconnect.IsBackground = true;
            t_clientDisconnect.Start();

            b_clientListener = true;
            t_clientListener = new Thread(ClientListenerThread);
            t_clientListener.IsBackground = true;
            t_clientListener.Start();

            _started = true;
            OnStart();

            Log.Information($"{_name} ServerCollector: started!");
        }
        public void Stop()
        {
            b_clientConnect = false;
            b_clientDisconnect = false;
            b_clientListener = false;

            t_clientConnect.Join();
            t_clientDisconnect.Join();
            t_clientListener.Join();

            tcpl_listener.Stop();

            _started = false;
            OnStop();

            Log.Information($"{_name} ServerCollector: stopped!");
        }

        private void ClientConnectThread()
        {
            while (b_clientConnect)
            {
                try
                {
                    TcpClient? client = null;

                    if (tcpl_listener.Pending())
                    {
                        client = tcpl_listener.AcceptTcpClient();

                        if (client != null)
                        {
                            string clientIp = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();
                            if (_clientNodes.Any(node => node.Ip == clientIp))
                            {
                                lock (_clientsLock)
                                {
                                    _clients.Add(client);
                                    Log.Information($"{_name} ServerCollector, client connected: '{_clientNodes.FirstOrDefault(n => n.Ip == clientIp).Name}', number of connected clients: '{_clients.Count}'");
                                }
                                OnClientConnected(client);
                            }
                            else
                            {
                                client.Dispose();
                            }
                        }
                    }

                    Thread.Sleep(_clientConnectThreadTimeoutMilliseconds);

                }

                catch (SocketException e)
                {
                    // This exception is expected when the server stops, so we just break the loop.
                    Log.Information($"{_name} ServerCollector: exception in ClientConnectThread: {e}");
                    //break; // ???
                }
            }
        }

        private void ClientDisconnectThread()
        {
            while (b_clientDisconnect)
            {
                lock (_clientsLock)
                {
                    if (_clients.Count != 0)
                    {
                        for (int i = _clients.Count - 1; i >= 0; i--)
                        {
                            TcpClient client = _clients[i];

                            if (!IsClientConnected(client))
                            {
                                string clientIp = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();
                                Log.Information($"{_name} ServerCollector, client disconnected: '{_clientNodes.FirstOrDefault(n => n.Ip == clientIp).Name}', number of connected clients: '{_clients.Count}'");
                                _clients.RemoveAt(i);
                                client.Close();
                                OnClientDisconnected(client);
                            }
                        }
                    }
                }
                Thread.Sleep(_clientDisconnectThreadTimeoutMilliseconds);
            }
        }
        private bool IsClientConnected(TcpClient client)
        {
            try
            {
                // Check if the client is connected by checking the state of the socket
                return !(client.Client.Poll(1, SelectMode.SelectRead) && client.Client.Available == 0);
            }
            catch (SocketException e)
            {
                // If there's a socket exception, the client is definitely not connected
                Log.Information($"{_name} ServerCollector: exception in IsClientConnected: {e}");
                return false;
            }
        }

        private void ClientListenerThread()
        {
            while (b_clientListener)
            {
                lock (_clientsLock)
                {
                    foreach (var client in _clients)
                    {
                        try
                        {
                            if (client.Available > 0) // Check if data is available
                            {
                                NetworkStream ns_stream = client.GetStream();
                                byte[] data = new byte[client.Available];
                                int bytesRead = ns_stream.Read(data, 0, data.Length);

                                if (bytesRead > 0) OnRecieved(client, data);
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Information($"{_name} ServerCollector: exception in ClientListenerThread: {ex}");
                        }
                    }
                }
                Thread.Sleep(_clientListenerThreadTimeoutMilliseconds);
            }
        }
    }
}