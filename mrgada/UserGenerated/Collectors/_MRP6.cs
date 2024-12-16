#pragma warning disable CS8981 // suppress naming rule violation
#pragma warning disable CS8618 // suppress non-null value when exiting constructor

public static partial class mrgada
{
    public static _MRP6? MRP6 = null;
    public partial class _MRP6 : S7Collector
    {
        public mrgada._MRP6.c_dbDigialValvesSCADA dbDigialValvesSCADA;
        public mrgada._MRP6.c_dbAnalogSensorsSCADA dbAnalogSensorsSCADA;
        public _MRP6(string name, int port, S7.Net.CpuType cpuType, string plcIp, short plcRack, short plcSlot, int readBroadcastProcessThreadMinIntervalMilliseconds = 100) : base(name, port, cpuType, plcIp, plcRack, plcSlot, readBroadcastProcessThreadMinIntervalMilliseconds)
        {
            dbDigialValvesSCADA = new(52, 792);
            dbAnalogSensorsSCADA = new(51, 2130, _s7ClientCollector, _s7Plc);

            AddS7db(dbDigialValvesSCADA);
            AddS7db(dbAnalogSensorsSCADA);
        }
    }
}