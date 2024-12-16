#pragma warning disable CS8601 // Possible null reference assignment.
using System;
using System.Collections.Generic;
using static mrgada;

public static partial class mrgada
{
    public static int NearestDivisible(int i, int x)
    {
        // i => Number for with we are seeking next nearest i % x == 0
        if (i % x == 0)
        {
            return i;
        }
        else
        {
            return ((i / x) + 1) * x;
        }
    }

    public class S7Var<T>
    {
        private T _cv;
        private T _cvOld;
        private T _lastHistorizedCv;
        private byte[] _cvBytes;
        private int _bitOffset;
        private short _bitAlligment;
        private int _bitsInVar;
        private S7Db _s7db;
        private S7ClientCollector _s7CollectorClient;
        private S7.Net.Plc _s7Plc;

        public S7Var(S7Db s7db, S7ClientCollector s7CollectorClient, S7.Net.Plc s7Plc)
        {
            _s7db = s7db;
            _s7CollectorClient = s7CollectorClient;
            _s7Plc = s7Plc;
            _cv = default(T);

            if (typeof(T) == typeof(bool)) _bitsInVar = 1;
            else if (typeof(T) == typeof(Int16)) _bitsInVar = 16;
            else if (typeof(T) == typeof(Int32)) _bitsInVar = 32;
            else if (typeof(T) == typeof(float)) _bitsInVar = 32;

            _cvBytes = new byte[(int)(_bitsInVar / 8)];
            if (typeof(T) == typeof(bool)) _cvBytes = new byte[1];
        }

        public event EventHandler<T> OnValueChanged;

        public T CV
        {
            get => _cv;
            set
            {
                if (!EqualityComparer<T>.Default.Equals(_cv, value))
                {
                    SetCV(value);
                }
            }
        }
        public void SetCV(T cv)
        {
            byte[] cvBytes = new byte[(int)(_bitsInVar / 8)];
            if (typeof(T) == typeof(bool)) cvBytes = new byte[1];

            if (cv == null) throw new Exception("CV cannot be null!");

            if (typeof(T) == typeof(bool)) cvBytes[0] = (byte)(Convert.ToByte((bool)(object)cv) << (_bitOffset % 8));
            else if (typeof(T) == typeof(Int16)) cvBytes = BitConverter.GetBytes((Int16)(object)cv);
            else if (typeof(T) == typeof(Int32)) cvBytes = BitConverter.GetBytes((Int32)(object)cv);
            else if (typeof(T) == typeof(float)) cvBytes = BitConverter.GetBytes((float)(object)cv);


            if (typeof(T) != typeof(bool) && BitConverter.IsLittleEndian) Array.Reverse(cvBytes);

            if (mrgada._nodeType == mrgada.NodeType.Client)
            {
                List<byte> send = new List<byte>();

                UInt16 dbNum = (UInt16)_s7db.Num;

                byte s7VarBitLength = 0;
                if (typeof(T) == typeof(bool)) s7VarBitLength = 1;
                else if (typeof(T) == typeof(Int16)) s7VarBitLength = 16;
                else if (typeof(T) == typeof(Int32)) s7VarBitLength = 32;
                else if (typeof(T) == typeof(float)) s7VarBitLength = 32;
                else return;
                
                // Build the structure: dbNum (16 bits) + bitOffset (32 bits) + s7VarBitLength (8 bits) + cvBytes (x bytes)
                send.AddRange(BitConverter.GetBytes(dbNum));

                send.AddRange(BitConverter.GetBytes((UInt32)_bitOffset));

                send.Add(s7VarBitLength);

                send.AddRange(cvBytes);

                _s7CollectorClient.AddToSendQueue(send.ToArray());

            }
            else
            {
                if (typeof(T) != typeof(bool)) {
                    var test = _s7Plc.ReadBytes(S7.Net.DataType.DataBlock, _s7db.Num, (int)(_bitOffset / 8), 4);
                    _s7Plc.WriteBytes(S7.Net.DataType.DataBlock, _s7db.Num, (int)(_bitOffset / 8), cvBytes);
            }
                else
                    _s7Plc.WriteBit(S7.Net.DataType.DataBlock, _s7db.Num, (int)(_bitOffset / 8), _bitOffset & 8, (bool)(object)cv);
            }
        }

        public void ParseCVs()
        {
            Array.Copy(_s7db.Bytes, _bitOffset / 8, _cvBytes, 0, _cvBytes.Length);
            if ((typeof(T) != typeof(bool)) && BitConverter.IsLittleEndian) { Array.Reverse(_cvBytes); }

            _cvOld = _cv;

            if (typeof(T) == typeof(bool)) _cv = (T)(object)((_cvBytes[0] & (1 << (_bitOffset % 8))) != 0);
            else if (typeof(T) == typeof(Int16)) _cv = (T)(object)BitConverter.ToInt16(_cvBytes, 0);
            else if (typeof(T) == typeof(Int32)) _cv = (T)(object)BitConverter.ToInt32(_cvBytes, 0);
            else if (typeof(T) == typeof(float)) _cv = (T)(object)BitConverter.ToSingle(_cvBytes, 0);

            // TODO problem za byte[]

            if (!EqualityComparer<T>.Default.Equals(_cvOld, _cv))
                OnValueChanged?.Invoke(this, _cv);
        }

        public int AlignAndIncrement(int bitOffset)
        {
            _bitAlligment = 8;
            if (typeof(T) == typeof(bool)) _bitAlligment = 1;
            else if (typeof(T) == typeof(Int16)) _bitAlligment = 16;
            else if (typeof(T) == typeof(Int32)) _bitAlligment = 16;
            else if (typeof(T) == typeof(float)) _bitAlligment = 16;

            _bitOffset = mrgada.NearestDivisible(bitOffset, _bitAlligment);
            return _bitOffset + _bitsInVar;
        }
    } 
}