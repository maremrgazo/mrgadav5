#pragma warning disable CS8981 // suppress naming rule violation
#pragma warning disable CS8618 // suppress non-null value when exiting constructor
#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Xml.Linq;
using Serilog;

public static partial class mrgada
{
    public class ClientCollector
    {
        protected readonly string _name;
        private readonly int _port;
        protected bool _started = false;
        protected volatile bool _connected = false;

        private volatile TcpClient _tcpClient;
        private volatile NetworkStream _networkStream;

        private Thread t_receiveHandler;
        private bool b_receiveHandler;
        private readonly int _receiveThreadTimeoutMilliseconds;

        private Thread t_connectHandler;
        private bool b_connectHandler;
        protected readonly int _connectHandlerTimeoutMilliseconds;

        private string _clientNodeName;

        public bool Started => _started;
        public bool Stopped => !_started;

        public bool Connected => _connected;
        public bool Disconnected => !_connected;

        public ClientCollector(string name, int serverPort, int connectHandlerTimeoutMilliseconds = 3000, int receiveThreadTimeoutMilliseconds = 100)
        {
            _name = name;
            _port = serverPort;

            _connectHandlerTimeoutMilliseconds = connectHandlerTimeoutMilliseconds;
            _receiveThreadTimeoutMilliseconds = receiveThreadTimeoutMilliseconds;

            _clientNodeName = _clientNodes.FirstOrDefault(n => (n.Ip == (Dns.GetHostAddresses(Dns.GetHostName()).FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork)).ToString())).Name;

            mrgada.AddClientCollector(this);
        }
        protected virtual void OnConnect() { }
        protected virtual void OnDisconnect() { }
        protected virtual void OnRecieved(byte[] data) { } // when client recieves server message
        protected virtual void OnStart() { }
        protected virtual void OnStop() { }

        public void Send(byte[] data) // send message to server
        {
            if (Disconnected || Stopped) return;
            try
            {
                _networkStream.Write(data, 0, data.Length);
            }
            catch (Exception ex)
            {
                Log.Warning($"{_name}: ClientCollector, exception in Send {ex}");
            }
        }

        public void Start()
        {
            b_connectHandler = true;
            t_connectHandler = new Thread(ConnectThread);
            t_connectHandler.IsBackground = true;
            t_connectHandler.Start();

            b_receiveHandler = true;
            t_receiveHandler = new Thread(ReceiveThread);
            t_receiveHandler.IsBackground = true;
            t_receiveHandler.Start();

            OnStart();

            Log.Information($"{_name} ClientCollector {_clientNodeName}: started!");
            _started = true;
        }
        public void Stop()
        {
            b_receiveHandler = false;
            b_connectHandler = false;

            t_connectHandler.Join();
            t_receiveHandler.Join();

            _networkStream?.Close();
            _tcpClient?.Close();

            OnStop();

            Log.Information($"{_name} ClientCollector: stopped!");
            _started = false;
        }
        private void ConnectThread()
        {
            while (b_connectHandler)
            {
                try
                {
                    if (!_connected)
                    {
                        _tcpClient = new TcpClient();
                        _tcpClient.Connect(IPAddress.Parse(mrgada._serverIp), _port);
                        _networkStream = _tcpClient.GetStream();

                        OnConnect();
                        _connected = true;

                        while (_connected && b_connectHandler)
                        {
                            try
                            {
                                _networkStream.Write(new byte[0], 0, 0);
                                Thread.Sleep(_connectHandlerTimeoutMilliseconds);
                            }
                            catch
                            {
                                OnDisconnect();
                                _connected = false;
                            }
                        }
                    }
                }
                catch
                {
                    Log.Information($"{_name} ClientCollector: retrying connection in ~ {_connectHandlerTimeoutMilliseconds / 1000.0:F2} seconds");
                    _connected = false;

                    Thread.Sleep(_connectHandlerTimeoutMilliseconds);
                }
            }
        }

        private void ReceiveThread()
        {
            try
            {
                while (b_receiveHandler)
                {
                    if (_connected)
                    {
                        if (_tcpClient.Available > 0)
                        {
                            byte[] buffer = new byte[_tcpClient.Available];
                            int bytesRead = _networkStream.Read(buffer, 0, buffer.Length);
                            if (bytesRead > 0)
                            {
                                OnRecieved(buffer);
                            }
                        }
                    }
                    Thread.Sleep(_receiveThreadTimeoutMilliseconds);
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"Receive thread encountered an error: {ex.Message}");
                _connected = false;
            }
        }


    }
}