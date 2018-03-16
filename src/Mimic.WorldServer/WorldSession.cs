using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using Mimic.Common;

namespace Mimic.WorldServer
{
    public class WorldSession
    {
        public static uint CLIENTCACHE_VERSION = 0; //load this from a config;

        public static uint STANDARD_ADDON_CRC = 0x4C1C776D;

        private IPAddress ip;
        private AccountInfo _info;

        private WorldHandler _wh;

        private AddonInfo[] _addonList;

        private uint[] _tutorialFlags;
        
        public WorldSession(int id, string name, WorldHandler wh, AccountInfo info){
            ip = (wh._client.Client.RemoteEndPoint as IPEndPoint).Address;
            _wh = wh;
            _info = info;
            _info.online=true;
            Program.authDatabase.AsyncUpdateAccount(_info);
        }

        public async Task LoadSessionData(){
            _tutorialFlags = await Program.authDatabase.AsyncFetchTutorialFlags(_info.id);
            if(_tutorialFlags == null){
                _tutorialFlags = new uint[8];
                Program.authDatabase.AsyncSetTutorialFlags(_info.id,_tutorialFlags);
            }
        }

        public async Task InitSession(){
            await LoadSessionData();
            SendAddonsInfo();
            SendClientCacheVersion();
            SendTutorialFlags();
        }

        public async Task HandlePacket(WorldPacket wp){
            
        }

        public void ReadAddonsInfo(byte[] data){
            //Debug.WriteLine(BitConverter.ToString(data).Replace("-",""));
            var addonData = new MemoryStream();
            using(var indata = new MemoryStream(data)){
                indata.Seek(6, SeekOrigin.Begin);
                using(var zlib = new DeflateStream(indata, CompressionMode.Decompress)){
                    zlib.CopyTo(addonData);
                }
            }
            //there must be a better way
            BinaryReader br = new BinaryReader(addonData);
            br.BaseStream.Position = 0;           

            uint addonCount = br.ReadUInt32();
            Debug.WriteLine(addonCount + " addons from the client");

            _addonList = new AddonInfo[addonCount]; 
            for(int i=0;i<addonCount;i++){
                AddonInfo _ainfo = new AddonInfo();
                var name = new MemoryStream();
                byte cur = (byte)br.ReadByte();
                while(cur != 0){
                    name.WriteByte(cur);
                    cur = (byte)br.ReadByte();
                }
                byte[] namearr = name.ToArray();
                _ainfo.name = Encoding.Default.GetString(namearr);
                _ainfo.enabled = br.ReadByte();
                _ainfo.crc = br.ReadUInt32();
                _ainfo.unk1 = br.ReadUInt32();
                _ainfo.state = 2;
                _ainfo.useCRCorPubKey = true;
                _addonList[i] = _ainfo;
            }
        }

        public void SendTutorialFlags(){
            WorldPacket wp = new WorldPacket(WorldCommand.SMSG_TUTORIAL_FLAGS,_wh);
            for(int i=0;i<8;i++){
                wp.append(_tutorialFlags[i]);
            }
            _wh._writer.Write(wp.result());
            _wh._writer.Flush();
        }
        public void SendClientCacheVersion(){
            WorldPacket wp = new WorldPacket(WorldCommand.SMSG_CLIENTCACHE_VERSION,_wh);
            wp.append(CLIENTCACHE_VERSION);
            _wh._writer.Write(wp.result());
            _wh._writer.Flush();
        }
        public void SendAddonsInfo()
        {
            byte[] addonPublicKey = {
                0xC3, 0x5B, 0x50, 0x84, 0xB9, 0x3E, 0x32, 0x42, 0x8C, 0xD0, 0xC7, 0x48, 0xFA, 0x0E, 0x5D, 0x54,
                0x5A, 0xA3, 0x0E, 0x14, 0xBA, 0x9E, 0x0D, 0xB9, 0x5D, 0x8B, 0xEE, 0xB6, 0x84, 0x93, 0x45, 0x75,
                0xFF, 0x31, 0xFE, 0x2F, 0x64, 0x3F, 0x3D, 0x6D, 0x07, 0xD9, 0x44, 0x9B, 0x40, 0x85, 0x59, 0x34,
                0x4E, 0x10, 0xE1, 0xE7, 0x43, 0x69, 0xEF, 0x7C, 0x16, 0xFC, 0xB4, 0xED, 0x1B, 0x95, 0x28, 0xA8,
                0x23, 0x76, 0x51, 0x31, 0x57, 0x30, 0x2B, 0x79, 0x08, 0x50, 0x10, 0x1C, 0x4A, 0x1A, 0x2C, 0xC8,
                0x8B, 0x8F, 0x05, 0x2D, 0x22, 0x3D, 0xDB, 0x5A, 0x24, 0x7A, 0x0F, 0x13, 0x50, 0x37, 0x8F, 0x5A,
                0xCC, 0x9E, 0x04, 0x44, 0x0E, 0x87, 0x01, 0xD4, 0xA3, 0x15, 0x94, 0x16, 0x34, 0xC6, 0xC2, 0xC3,
                0xFB, 0x49, 0xFE, 0xE1, 0xF9, 0xDA, 0x8C, 0x50, 0x3C, 0xBE, 0x2C, 0xBB, 0x57, 0xED, 0x46, 0xB9,
                0xAD, 0x8B, 0xC6, 0xDF, 0x0E, 0xD6, 0x0F, 0xBE, 0x80, 0xB3, 0x8B, 0x1E, 0x77, 0xCF, 0xAD, 0x22,
                0xCF, 0xB7, 0x4B, 0xCF, 0xFB, 0xF0, 0x6B, 0x11, 0x45, 0x2D, 0x7A, 0x81, 0x18, 0xF2, 0x92, 0x7E,
                0x98, 0x56, 0x5D, 0x5E, 0x69, 0x72, 0x0A, 0x0D, 0x03, 0x0A, 0x85, 0xA2, 0x85, 0x9C, 0xCB, 0xFB,
                0x56, 0x6E, 0x8F, 0x44, 0xBB, 0x8F, 0x02, 0x22, 0x68, 0x63, 0x97, 0xBC, 0x85, 0xBA, 0xA8, 0xF7,
                0xB5, 0x40, 0x68, 0x3C, 0x77, 0x86, 0x6F, 0x4B, 0xD7, 0x88, 0xCA, 0x8A, 0xD7, 0xCE, 0x36, 0xF0,
                0x45, 0x6E, 0xD5, 0x64, 0x79, 0x0F, 0x17, 0xFC, 0x64, 0xDD, 0x10, 0x6F, 0xF3, 0xF5, 0xE0, 0xA6,
                0xC3, 0xFB, 0x1B, 0x8C, 0x29, 0xEF, 0x8E, 0xE5, 0x34, 0xCB, 0xD1, 0x2A, 0xCE, 0x79, 0xC3, 0x9A,
                0x0D, 0x36, 0xEA, 0x01, 0xE0, 0xAA, 0x91, 0x20, 0x54, 0xF0, 0x72, 0xD8, 0x1E, 0xC7, 0x89, 0xD2
            };
            
            WorldPacket wp = new WorldPacket(WorldCommand.SMSG_ADDON_INFO,_wh);
            foreach (var addon in _addonList)
            {
                wp.append(addon.state);
                if(addon.useCRCorPubKey){
                    byte usePK = (byte)(addon.crc != STANDARD_ADDON_CRC?1:0);
                    wp.append(usePK);
                    if(usePK==1){
                        wp.append(addonPublicKey);
                    }
                    wp.append(0);
                }
                wp.append((byte)0);
            }


            wp.append(0); //No banned addons for now. eventually...

            //this is the correct impl
            /*
            wp.append(AddonMgr._bannedAddons.length);
            foreach(var banned in AddonMgr._bannedAddons){
                wp.append(banned.id);
                wp.append(banned.nameMD5);
                wp.append(banned.versionMD5);
                wp.append(banned.timestamp);
                wp.append(banned.isBanned); //should be 1
            }*/

            _wh._writer.Write(wp.result());
            _wh._writer.Flush();
        }
    }
}
