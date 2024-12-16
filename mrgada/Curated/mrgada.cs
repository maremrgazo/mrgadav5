#pragma warning disable CS8981 // suppress naming rule violation
#pragma warning disable CS8618 // suppress non-null value when exiting constructor

using Serilog;
using S7.Net;
using System.Collections.ObjectModel;

public static partial class mrgada
{
    private static string _serverIp;
    private static int _serverPort;
    private static NodeType _nodeType;
    private static bool _started = false;

    public enum NodeType
    {
        Server,
        Client
    }

    public enum PlcType
    {
        S7_1200 = S7.Net.CpuType.S71200,
        S7_1500 = S7.Net.CpuType.S71500
    }

    public static void Init(string serverIp, int serverPort, NodeType nodeType)
    {
        _serverIp = serverIp;
        _serverPort = serverPort;
        _nodeType = nodeType;

        Log.Logger = new LoggerConfiguration()
        .WriteTo.Console()
        .MinimumLevel.Debug()
        .CreateLogger();
    }
    public class Node
    {
        public readonly string Ip;
        public readonly string Name;
        public Node(string ip, string name)
        {
            Ip = ip;
            Name = name;
        }
    }
    private static List<Node> _clientNodes = [];
    public static void AddClientNode(Node node)
    {
        _clientNodes.Add(node);
    }

    private static List<ServerCollector> _serverCollectors = [];
    private static List<ClientCollector> _clientCollectors = [];
    public static void AddServerCollector(ServerCollector serverCollector) { _serverCollectors.Add(serverCollector); }
    public static void AddClientCollector(ClientCollector clientCollector) { _clientCollectors.Add(clientCollector); }
    public static void Start()
    {
        Log.Information("mrgada: Started!");
        switch (mrgada._nodeType)
        {
            case NodeType.Server:
                if (_serverCollectors.Count <= 0) Log.Warning("mrgada: No collectors were initialized!");
                foreach (ServerCollector serverCollector in _serverCollectors) serverCollector.Start();
                break;
            case NodeType.Client:
                if (_clientCollectors.Count <= 0) Log.Warning("mrgada: No collectors were initialized!");
                foreach (ClientCollector clientCollector in _clientCollectors) clientCollector.Start();
                break;
        }
    }
    public static void Stop()
    {
        Log.Information("mrgada: Stopped!");
        switch (mrgada._nodeType)
        {
            case NodeType.Server:
                foreach (ServerCollector serverCollector in _serverCollectors) serverCollector.Stop();
                break;
            case NodeType.Client:
                foreach (ClientCollector clientCollector in _clientCollectors) clientCollector.Stop();
                break;
        }
    }
}