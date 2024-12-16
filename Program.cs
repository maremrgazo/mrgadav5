public class Program
{
    public static void Main(string[] args)
    {
        mrgada.Init("192.168.64.113", 61000, mrgada.NodeType.Client);

        mrgada.AddClientNode(new("192.168.64.122", "clientA"));
        mrgada.AddClientNode(new("192.168.64.113", "debugging"));

        mrgada.MRP6 = new("MRP6", 61101, S7.Net.CpuType.S71500, "192.168.64.177", 0, 1, 2000);

        mrgada.Start();

        Thread.Sleep(Timeout.Infinite);
    }
}