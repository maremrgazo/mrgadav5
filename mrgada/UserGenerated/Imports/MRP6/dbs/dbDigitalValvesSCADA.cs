
using static mrgada.S7Collector;

public static partial class mrgada
{
    public partial class _MRP6
    {
        public class c_dbDigialValvesSCADA: S7Db
        {
            public S7Var<byte> FT_6IN_702;
            public c_dbDigialValvesSCADA(int num, int len) : base(num, len)
            {
            }

            public override void ParseCVs()
            {
            }
        }
    }
}