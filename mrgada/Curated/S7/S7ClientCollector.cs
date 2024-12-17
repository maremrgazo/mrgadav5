#pragma warning disable CS8981 // suppress naming rule violation
#pragma warning disable CS8618 // suppress non-null value when exiting constructor
#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.

using S7.Net;
using Serilog;
using SerilogTimings;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static mrgada;

public static partial class mrgada
{
    public class S7ClientCollector:ClientCollector
    {
        private List<mrgada.S7Db> _s7PlcDbs;

        private List<byte> _send = [];
        private Thread? t_send;
        private bool b_send;
        private object o_sendLock = new();
        private int i_sendTimeout = 200;
        private bool _plcConnected = false;
        public bool PlcConnected => _plcConnected;

        public S7ClientCollector(string name, int port, List<mrgada.S7Db> s7PlcDbs) :base(name, port)
        {
            _s7PlcDbs = s7PlcDbs;
        }
        public void AddToSendQueue(byte[] data)
        {
            lock (o_sendLock)
            {
                _send.AddRange(data);
            }
        }

        protected override void OnStart()
        {
            t_send = new(SendThread);
            t_send.IsBackground = true;
            t_send.Start();

            b_send = true;
        }
        protected override void OnStop()
        {
            b_send = false;
            t_send.Join();
        }

        private void SendThread()
        {
            while (b_send)
            {
                if (_connected)
                {
                    lock (o_sendLock)
                    {
                        if (_send.Count > 0)
                        {
                            List<byte> _finallSend = [];
                                Int32 chunkLength = sizeof(Int32) + _send.Count;
                                _send.InsertRange(0, BitConverter.GetBytes((Int32)chunkLength));
                                _finallSend.AddRange(_send);
                                _send.Clear();
                            Send(_finallSend.ToArray());
                        }
                    }
                    Thread.Sleep(i_sendTimeout);
                }
                else
                {
                    Thread.Sleep(_connectHandlerTimeoutMilliseconds);
                }
            }
        }
        protected override void OnRecieved(byte[] data)
        {
            Int32 broadcastLength = BitConverter.ToInt32(data, 0);
            bool isPartial = data.Length != broadcastLength;

            Log.Information($"{mrgada.ClientNodeName}: Client Recieved Broadcast, len ({data.Length}) from S7 ServerCollector: {_name}");

            int i = sizeof(Int32); // skip plcConnected
            while (i < data.Length)
            {
                short i16_chunkLength = BitConverter.ToInt16(data, i);
                short i16_dbNumber = BitConverter.ToInt16(data, i + sizeof(Int16));
                byte[] ba_dbBytes = new byte[i16_chunkLength - (sizeof(Int16) + sizeof(Int16))];
                Buffer.BlockCopy(data, i + sizeof(Int16) + sizeof(Int16), ba_dbBytes, 0, ba_dbBytes.Length);

                foreach (mrgada.S7Db s7Db in _s7PlcDbs)
                {
                    if (s7Db.Num == i16_dbNumber)
                    {
                        s7Db.SetBytes(ba_dbBytes);
                        s7Db.ParseCVs();
                        Log.Information($"  Client chunk, db ({i16_dbNumber}), len ({ba_dbBytes.Length}) from S7 Collector: {_name}");

                        // Get last 10 bytes of db and log
                        int lengthToTake = Math.Min(10, ba_dbBytes.Length);
                        byte[] lastBytes = new byte[lengthToTake];
                        Array.Copy(ba_dbBytes, ba_dbBytes.Length - lengthToTake, lastBytes, 0, lengthToTake);
                        string s_lastBytes = BitConverter.ToString(lastBytes).Replace("-", " "); ;
                        Log.Information($"      Last ~ 10 bytes are, {s_lastBytes} ");

                        break;
                    }
                }
                i += i16_chunkLength;
            }
        }
    }
}