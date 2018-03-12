using System;
using System.Text;
using System.Collections.Generic;

namespace Mimic.WorldServer
{
    public class WorldPacket
    {
        private WorldCommand cmd;
        public int len;

        private List<byte> data;

        public WorldPacket(WorldCommand cmd)
        {
            data = new List<byte>();
            append((UInt16)0);
            append((UInt16)cmd);
        }

        public void append(byte b)
        {
            data.Add(b);
        }
        public void append(byte[] bs, bool le=true)
        {
            if(le != BitConverter.IsLittleEndian)
            {
                Array.Reverse(bs);
            }
            data.AddRange(bs);
        }
        public void append(String s)
        {
            data.AddRange(Encoding.UTF8.GetBytes(s + "\x0"));
        }
        public void append(UInt16 d)
        {
            append(BitConverter.GetBytes(d));
        }
        public void append(UInt32 d)
        {
            append(BitConverter.GetBytes(d));
        }
        public void append(UInt64 d)
        {
            append(BitConverter.GetBytes(d));
        }
        public void append(Int16 d)
        {
            append(BitConverter.GetBytes(d));
        }
        public void append(Int32 d)
        {
            append(BitConverter.GetBytes(d));
        }
        public void append(Int64 d)
        {
            append(BitConverter.GetBytes(d));
        }

        public byte[] result()
        {
            int len = data.Count - 2;
            byte lenb1 = (byte)(len & 0xff);
            byte lenb2 = (byte)(len << 8);
            data[1] = lenb1;
            data[0] = lenb2;
            return data.ToArray();
        }
    }
}
