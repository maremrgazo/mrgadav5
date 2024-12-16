#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.

using S7.Net;
using System.Net.Sockets;
using System.Net;
using static mrgada;
using System.Collections.Generic;

public static partial class mrgada
{
    public class S7Collector
    {
        private readonly string _name;
        private readonly int _port;
        private readonly string _plcIp;
        private readonly S7.Net.CpuType _cpuType;
        private readonly short _plcRack;
        private readonly short _plcSlot;
        protected S7.Net.Plc _s7Plc;

        private bool _plcConnected = false;
        public bool PlcConnected => _plcConnected;

        private S7ServerCollector _s7ServerCollector;
        protected S7ClientCollector _s7ClientCollector;
        private List<S7Db> _s7PlcDbs = [];

        private string _clientNodeName;


        public S7Collector(string name, int port, S7.Net.CpuType cpuType, string plcIp, short plcRack, short plcSlot, int readBroadcastProcessThreadMinIntervalMilliseconds = 100)
        {
            _name = name;
            _port = port;
            _plcIp = plcIp;
            _cpuType = cpuType;
            _plcRack = plcRack;
            _plcSlot = plcSlot;

            _s7Plc = new(_cpuType, _plcIp, _plcRack, _plcSlot);

            switch (mrgada._nodeType)
            {
                case mrgada.NodeType.Server:
                    _s7ServerCollector = new(_name, _port, _s7Plc, _s7PlcDbs, _plcConnected, readBroadcastProcessThreadMinIntervalMilliseconds);
                    break;
                case mrgada.NodeType.Client:
                    _s7ClientCollector = new(_name, _port, _s7PlcDbs, _plcConnected);
                    break;
            }
        }

        protected void AddS7db(mrgada.S7Db s7db)
        {
            _s7PlcDbs.Add(s7db);
        }
    }
}