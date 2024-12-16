using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static mrgada;

public partial class mrgada
{
    public partial class _MRP6 : S7Collector
    {
        public class udtSCADAAnalogSensor
        {
            public S7Var<float> ValueEgu;
            public S7Var<bool> Failure;
            public S7Var<bool> Manual;
            public S7Var<bool> WarningEnabled;
            public S7Var<bool> WarningActive;
            public S7Var<bool> WarningValueLow;
            public S7Var<bool> WarningValueHigh;
            public S7Var<bool> WarningTimeOut;
            public S7Var<bool> Spare_Bool_0;
            public S7Var<bool> InitWarnings;
            public S7Var<bool> ToggleWarnings;
            public S7Var<bool> WarningEnabledW;
            public S7Var<bool> WarningValueLowW;
            public S7Var<bool> WarningValueHighW;
            public S7Var<bool> ToggleWarningsW;
            public S7Var<bool> Spare_Bool_1;
            public S7Var<bool> Spare_Bool_2;
            public S7Var<float> ManValueEGU;

            private S7Db _s7db;
            private S7ClientCollector _s7ClientCollector;
            private S7.Net.Plc _s7Plc;

            private List<S7Var<object>> _S7Vars;

            public udtSCADAAnalogSensor(S7Db s7db, S7ClientCollector s7ClientCollector, S7.Net.Plc s7Plc)
            {
                _s7db = s7db;
                _s7ClientCollector = s7ClientCollector;
                _s7Plc = s7Plc;

                ValueEgu = new(s7db, _s7ClientCollector, _s7Plc);
                Failure = new(s7db, _s7ClientCollector, _s7Plc);
                Manual = new(s7db, _s7ClientCollector, _s7Plc);
                WarningEnabled = new(s7db, _s7ClientCollector, _s7Plc);
                WarningActive = new(s7db, _s7ClientCollector, _s7Plc);
                WarningValueLow = new(s7db, _s7ClientCollector, _s7Plc);
                WarningValueHigh = new(s7db, _s7ClientCollector, _s7Plc);
                WarningTimeOut = new(s7db, _s7ClientCollector, _s7Plc);
                Spare_Bool_0 = new(s7db, _s7ClientCollector, _s7Plc);
                InitWarnings = new(s7db, _s7ClientCollector, _s7Plc);
                ToggleWarnings = new(s7db, _s7ClientCollector, _s7Plc);
                WarningEnabledW = new(s7db, _s7ClientCollector, _s7Plc);
                WarningValueLowW = new(s7db, _s7ClientCollector, _s7Plc);
                WarningValueHighW = new(s7db, _s7ClientCollector, _s7Plc);
                ToggleWarningsW = new(s7db, _s7ClientCollector, _s7Plc);
                Spare_Bool_1 = new(s7db, _s7ClientCollector, _s7Plc);
                Spare_Bool_2 = new(s7db, _s7ClientCollector, _s7Plc);
                ManValueEGU = new(s7db, _s7ClientCollector, _s7Plc);
            }
            public int AlignAndIncrement(int bitOffset)
            {
                bitOffset = ValueEgu.AlignAndIncrement(bitOffset);
                bitOffset = Failure.AlignAndIncrement(bitOffset);
                bitOffset = Manual.AlignAndIncrement(bitOffset);
                bitOffset = WarningEnabled.AlignAndIncrement(bitOffset);
                bitOffset = WarningActive.AlignAndIncrement(bitOffset);
                bitOffset = WarningValueLow.AlignAndIncrement(bitOffset);
                bitOffset = WarningValueHigh.AlignAndIncrement(bitOffset);
                bitOffset = WarningTimeOut.AlignAndIncrement(bitOffset);
                bitOffset = Spare_Bool_0.AlignAndIncrement(bitOffset);
                bitOffset = InitWarnings.AlignAndIncrement(bitOffset);
                bitOffset = ToggleWarnings.AlignAndIncrement(bitOffset);
                bitOffset = WarningEnabledW.AlignAndIncrement(bitOffset);
                bitOffset = WarningValueLowW.AlignAndIncrement(bitOffset);
                bitOffset = WarningValueHighW.AlignAndIncrement(bitOffset);
                bitOffset = ToggleWarningsW.AlignAndIncrement(bitOffset);
                bitOffset = Spare_Bool_1.AlignAndIncrement(bitOffset);
                bitOffset = Spare_Bool_2.AlignAndIncrement(bitOffset);
                bitOffset = ManValueEGU.AlignAndIncrement(bitOffset);

                return bitOffset;
            }
            public void ParseCVs(byte[] bytes)
            {
                ValueEgu.ParseCVs();
                Failure.ParseCVs();
                Manual.ParseCVs();
                WarningEnabled.ParseCVs();
                WarningActive.ParseCVs();
                WarningValueLow.ParseCVs();
                WarningValueHigh.ParseCVs();
                WarningTimeOut.ParseCVs();
                Spare_Bool_0.ParseCVs();
                InitWarnings.ParseCVs();
                ToggleWarnings.ParseCVs();
                WarningEnabledW.ParseCVs();
                WarningValueLowW.ParseCVs();
                WarningValueHighW.ParseCVs();
                ToggleWarningsW.ParseCVs();
                Spare_Bool_1.ParseCVs();
                Spare_Bool_2.ParseCVs();
                ManValueEGU.ParseCVs();
            }
        }
    }
}
