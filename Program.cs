using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using static Program;

public class Program
{
public class MRP6Config
{
public string Name { get; set; }
public int Port { get; set; }
public string CpuType { get; set; }
public string Ip { get; set; }
public short Rack { get; set; }
public short Slot { get; set; }
public int Timeout { get; set; }
}

public class ClientNodeConfig
{
public string Ip { get; set; }
public string Name { get; set; }
}

public class MrgadaConfig
{
public string ServerIp { get; set; }
public int ServerPort { get; set; }
public string NodeType { get; set; }
public List<ClientNodeConfig> ClientNodes { get; set; }
public MRP6Config MRP6 { get; set; }
}
    public static void Main(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory()) // or use a known path if needed
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        var mrgadaConfig = configuration.GetSection("Mrgada").Get<MrgadaConfig>();
        mrgada.NodeType nodeType = mrgadaConfig.NodeType == "Server" ? mrgada.NodeType.Server : mrgada.NodeType.Client;

        mrgada.Init(mrgadaConfig.ServerIp, mrgadaConfig.ServerPort, nodeType);

        foreach (var clientNode in mrgadaConfig.ClientNodes)
        {
            mrgada.AddClientNode(new(clientNode.Ip, clientNode.Name));
        }

        var cpuType = Enum.Parse<S7.Net.CpuType>(mrgadaConfig.MRP6.CpuType);

        mrgada.MRP6 = new(
            mrgadaConfig.MRP6.Name,
            mrgadaConfig.MRP6.Port,
            S7.Net.CpuType.S71500,
            mrgadaConfig.MRP6.Ip,
            mrgadaConfig.MRP6.Rack,
            mrgadaConfig.MRP6.Slot,
            mrgadaConfig.MRP6.Timeout
        );

        mrgada.Start();

        if (nodeType == mrgada.NodeType.Server)
        {
            Historian.Start();
        }

        Thread.Sleep(Timeout.Infinite);
    }
}