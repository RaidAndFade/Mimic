using System;
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Diagnostics;

namespace Mimic.WorldServer
{
    public class WorldPacket
    {
        public WorldCommand cmd;
        private WorldHandler wh;

        private List<byte> data;


        public WorldPacket(WorldCommand cmd, WorldHandler wh)
        {
            this.wh = wh;
            this.cmd = cmd;
            data = new List<byte>();
            append((UInt16)0);
            append((UInt16)cmd);
        }

        public WorldPacket(byte[] data, WorldHandler wh)
        {
            this.wh = wh;
            this.data = new List<byte>();
            this.data.AddRange(data);

            byte[] cmdbuffer = {data[0],data[1],data[2],data[3]};
            byte[] decCMD = wh._ac.decrypt(cmdbuffer);
            this.data[0] = decCMD[0];
            this.data[1] = decCMD[1];
            this.data[2] = decCMD[2];
            this.data[3] = decCMD[3];
            this.cmd = (WorldCommand)BitConverter.ToUInt32(decCMD,0);
            this._rpos=4;
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
            var dataArr = data.ToArray();

            int len = dataArr.Length - 2;
            byte lenb1 = (byte)(len & 0xff);
            byte lenb2 = (byte)(len << 8);



            dataArr[1] = lenb1;
            dataArr[0] = lenb2;
            Debug.WriteLine(BitConverter.ToString(dataArr).Replace("-", ""));

            var header = wh._ac.encrypt(dataArr.Take(4).ToArray());
            Debug.WriteLine(BitConverter.ToString(header).Replace("-", ""));
            var res = header.Concat(dataArr.Skip(4)).ToArray();
            Debug.WriteLine(BitConverter.ToString(res).Replace("-", ""));
            return res;
        }
    }
}
