using System;
using System.Text;
using System.Collections.Generic;

namespace Mimic.WorldServer
{
    public class WorldPacket
    {
        private WorldCommand cmd;
        private WorldHandler wh;

        private List<byte> data;


        public WorldPacket(WorldCommand cmd, WorldHandler wh)
        {
            this.wh = wh;
            data = new List<byte>();
            append((UInt16)0);
            append((UInt16)cmd);
        }

        public WorldPacket(byte[] data, WorldHandler wh)
        {
            this.wh = wh;
            this.data = new List<byte>();
            this.data.AddRange(wh._ac.decrypt(data));
        }

        public int _rpos;
        public int Length { get => data.Count; }

        public byte ReadByte()
        {
            if(_rpos+1 > data.Count)
            {
                throw new IndexOutOfRangeException();
            }
            return data[_rpos++];
        }
        public byte[] ReadBytes(int count, bool le=true)
        {

            if (_rpos + count > data.Count)
            {
                throw new IndexOutOfRangeException();
            }
            List<byte> bytes = new List<byte>();
            while (count-- > 0)
            {
                bytes.Add(ReadByte());
            }
            byte[] bs = bytes.ToArray();
            if (le != BitConverter.IsLittleEndian)
            {
                Array.Reverse(bs);
            }
            return bs;
        }
        public sbyte ReadSByte()
        {
            return (sbyte)ReadByte();
        }
        public short ReadInt16()
        {
            return BitConverter.ToInt16(ReadBytes(2), 0);
        }
        public ushort ReadUInt16()
        {
            return BitConverter.ToUInt16(ReadBytes(2), 0);
        }
        public uint ReadUInt32()
        {
            return BitConverter.ToUInt32(ReadBytes(4), 0);
        }
        public int ReadInt32()
        {
            return BitConverter.ToInt32(ReadBytes(4), 0);
        }
        public ulong ReadUInt64()
        {
            return BitConverter.ToUInt32(ReadBytes(8), 0);
        }
        public long ReadInt64()
        {
            return BitConverter.ToInt32(ReadBytes(8), 0);
        }
        public string ReadString()
        {
            string s = "";
            byte cb = 0;
            int l = data.Count;
            while (_rpos<l)
            {
                cb = ReadByte();
                if (cb == 0)
                    break;
                s += (char)cb;
            }
            return s;
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
            return wh._ac.encrypt(data.ToArray());
        }
    }
}
