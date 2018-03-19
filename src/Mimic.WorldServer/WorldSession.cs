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
        public static uint CLIENTCACHE_VERSION = 64; //load this from a config;
        public static uint STANDARD_ADDON_CRC = 0x4C1C776D;
        public static byte GLOBAL_CACHE_MASK = 0x15;
        public static byte CHARACTER_CACHE_MAX = 0xEA;

        private IPAddress ip;
        private AccountInfo _info;

        public WorldHandler _wh;

        private AddonInfo[] _addonList;

        private AccountTutorialFlags _tutorialFlags;
        private AccountData[] _data;
        private Character[] _charcache;
        private bool _loaded = false;

        private OpcodeHandler ophandler;

        private Queue<WorldPacket> wp;
        
        public WorldSession(WorldHandler wh, AccountInfo info){
            ip = (wh._client.Client.RemoteEndPoint as IPEndPoint).Address;
            _wh = wh;
            _info = info;
            _info.online=true;
            Program.authDatabase.Accounts.Update(_info);
            ophandler = new OpcodeHandler(this);
        }

        public async Task InitSession(){
            LoadSessionData();
            SendAddonsInfo();
            SendClientCacheVersion();
            SendTutorialFlags();
        }

        public async Task HandlePacket(WorldPacket wp){
            ophandler.handle(wp);
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

       /******************************************\
        *  _____                      _           *
        * |  __ \                    (_)       _  *
        * | |  \/ ___ _ __   ___ _ __ _  ___  (_) *
        * | | __ / _ \ '_ \ / _ \ '__| |/ __|     *
        * | |_\ \  __/ | | |  __/ |  | | (__   _  *
        *  \____/\___|_| |_|\___|_|  |_|\___| (_) *
        *                                         *
        \*****************************************/
        public void LoadSessionData(){
            if(_loaded)return;
            try{
                _tutorialFlags = Program.authDatabase.Account_Tutorial.Single(f=>f.id==_info.id);
            }catch(InvalidOperationException ioe){
                _tutorialFlags = new AccountTutorialFlags{id = _info.id, tut0 = 0, tut1 = 0, tut2 = 0, tut3 = 0, tut4 = 0, tut5 = 0, tut6 = 0, tut7 = 0};
                Program.authDatabase.Account_Tutorial.Add(_tutorialFlags);
                Program.authDatabase.SaveChangesAsync();
            }
            _data = new AccountData[Consts.ACCOUNT_DATA_TYPE_LEN];
            for(var i=0;i<Consts.ACCOUNT_DATA_TYPE_LEN;i++){
                _data[i] = new AccountData{ account=_info.id, data={}, type=(byte)i, time=0 };
            }
            try{
                List<AccountData> datas = Program.authDatabase.Account_Data.Where(d=>d.account == _info.id).ToList();
                foreach(var data in datas){
                    _data[data.type] = data;
                }
            }catch(InvalidOperationException ioe){

            }
            LoadCharacters();
            _loaded=true;
        }

        public void LoadCharacters(){
            _charcache = Program.worldDatabase.Characters.Where(c=>c.account==_info.id&&c.deleteInfo_name==null).ToArray();
        }

        public void Update(long diff){

        }

        /***************************************************************************\
        * ______          _        _     _   _                 _ _                  *
        * | ___ \        | |      | |   | | | |               | | |               _ *
        * | |_/ /_ _  ___| | _____| |_  | |_| | __ _ _ __   __| | | ___ _ __ ___ (_)*
        * |  __/ _` |/ __| |/ / _ \ __| |  _  |/ _` | '_ \ / _` | |/ _ \ '__/ __|   *
        * | | | (_| | (__|   <  __/ |_  | | | | (_| | | | | (_| | |  __/ |  \__ \ _ *
        * \_|  \__,_|\___|_|\_\___|\__| \_| |_/\__,_|_| |_|\__,_|_|\___|_|  |___/(_)*
        *                                   SEND                                    *
        \***************************************************************************/
        public void SendTutorialFlags(){
            WorldPacket wp = new WorldPacket(WorldCommand.SMSG_TUTORIAL_FLAGS,this);
            wp.append(_tutorialFlags.tut0);
            wp.append(_tutorialFlags.tut1);
            wp.append(_tutorialFlags.tut2);
            wp.append(_tutorialFlags.tut3);
            wp.append(_tutorialFlags.tut4);
            wp.append(_tutorialFlags.tut5);
            wp.append(_tutorialFlags.tut6);
            wp.append(_tutorialFlags.tut7);
            _wh._writer.Write(wp.result());
        }
        public void SendClientCacheVersion(){
            WorldPacket wp = new WorldPacket(WorldCommand.SMSG_CLIENTCACHE_VERSION,this);
            wp.append(CLIENTCACHE_VERSION);
            _wh._writer.Write(wp.result());
        }
        public void SendAddonsInfo()
        {
            Debug.WriteLine("SENDING ADDONINFO");
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
                wp.append((byte)(addon.useCRCorPubKey?1:0));
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
        }

        public void SendDataTimes(uint mask){
            Debug.WriteLine("SENDING DATATIMES");
            WorldPacket wp = new WorldPacket(WorldCommand.SMSG_ACCOUNT_DATA_TIMES,this);
            wp.append((uint)DateTimeOffset.Now.ToUnixTimeSeconds()); //TODO GameTime::GetGameTime() (serverwide time intervals)
            wp.append((byte)1);
            wp.append(mask);
            for(int i=0;i<Consts.ACCOUNT_DATA_TYPE_LEN;i++){
                if((mask & (1 << i)) != 0){
                    wp.append((uint)_data[i].time);
                }
            }
            _wh._writer.Write(wp.result());
        }

        public void SendRealmSplit(WorldPacket req){
            Debug.WriteLine("SENDING REALMSPLIT");
            Task.Delay(2000);
            WorldPacket wp = new WorldPacket(WorldCommand.SMSG_REALM_SPLIT,this);
            string splitDate = "01/01/01";
            wp.append(req.ReadInt32()); //unk checksum or something
            wp.append(0);
            wp.append(splitDate);
            _wh._writer.Write(wp.result());
        }

        public void SendCharEnum(){
            //LoadCharacters();
            Task.Delay(4000); //i wish i knew...
            _charcache = new Character[1];
            _charcache[0]=
                new Character{
                    guid = 1,
                    name="Abb",
                    race=7,
                    charclass=4,
                    gender=0,
                    skin=4,
                    face=6,
                    hairStyle=0,
                    hairColor=0,
                    facialStyle=1,
                    level=1,
                    zone=1,
                    map=0,
                    posx=-6240f,
                    posy=331f,
                    posz=382.783f,
                    flags=0,
                    at_login=0,
                    equipmentCache="0 0 0 0 0 0 6096 0 56 0 0 0 1395 0 55 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 35 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0"
            };

            WorldPacket wp = new WorldPacket(WorldCommand.SMSG_CHAR_ENUM,this);
            wp.append((byte)_charcache.Length);

            foreach(var chr in _charcache){
                wp.append((ulong)chr.guid);
                wp.append(chr.name);
                wp.append(chr.race);
                wp.append(chr.charclass);
                wp.append(chr.gender);
                wp.append(chr.skin);
                wp.append(chr.face);
                wp.append(chr.hairStyle);
                wp.append(chr.hairColor);
                wp.append(chr.facialStyle);
                wp.append((byte)chr.level);
                wp.append((uint)chr.zone);
                wp.append((uint)chr.map);
                wp.append(chr.posx);
                wp.append(chr.posy);
                wp.append(chr.posz);
                wp.append(0); //guild_id (-1)

                var playerFlags = chr.flags;
                var atloginFlags = chr.at_login;
                uint charFlags = 0;

                //TODO if styles and class+race do not work, demand fix.

                //PlayerFlags & CharFlags
                if((atloginFlags & (ushort)AtLoginFlags.AT_LOGIN_RESURRECT) != 0)
                    playerFlags &= ~(uint)PlayerFlags.PLAYER_FLAGS_GHOST;
                if((atloginFlags & (ushort)AtLoginFlags.AT_LOGIN_RENAME) != 0)
                    charFlags &= ~(uint)CharacterFlags.CHARACTER_FLAG_RENAME;

                if((playerFlags & (ushort)PlayerFlags.PLAYER_FLAGS_HIDE_HELM) != 0)
                    charFlags |= (uint)CharacterFlags.CHARACTER_FLAG_HIDE_HELM;
                if((playerFlags & (ushort)PlayerFlags.PLAYER_FLAGS_HIDE_CLOAK) != 0)
                    charFlags |= (uint)CharacterFlags.CHARACTER_FLAG_HIDE_CLOAK;

                if((playerFlags & (ushort)PlayerFlags.PLAYER_FLAGS_GHOST) != 0)
                    charFlags |= (uint)CharacterFlags.CHARACTER_FLAG_GHOST;
                
                //TODO declined name
                wp.append(charFlags);
                
                if((atloginFlags & (ushort)AtLoginFlags.AT_LOGIN_CUSTOMIZE) != 0)
                    wp.append((uint)CharacterCustomizeFlags.CHAR_CUSTOMIZE_FLAG_CUSTOMIZE);
                else if((atloginFlags & (ushort)AtLoginFlags.AT_LOGIN_CHANGE_FACTION) != 0)
                    wp.append((uint)CharacterCustomizeFlags.CHAR_CUSTOMIZE_FLAG_RACE);
                else if((atloginFlags & (ushort)AtLoginFlags.AT_LOGIN_CHANGE_RACE) != 0) //cdd70bc6 357e04c3 f90fa742
                    wp.append((uint)CharacterCustomizeFlags.CHAR_CUSTOMIZE_FLAG_RACE);
                else
                    wp.append((uint)CharacterCustomizeFlags.CHAR_CUSTOMIZE_FLAG_NONE);
                
                wp.append((byte)((atloginFlags & (uint)AtLoginFlags.AT_LOGIN_FIRST) != 0?1:0));

                uint petDisplayId = 0;
                uint petLevel = 0;
                uint petFamily = 0;

                wp.append(petDisplayId);
                wp.append(petLevel);
                wp.append(petFamily);
                //get player pet, if not dead(not player_flags_ghost)

                var items = chr.equipmentCache.Split(" ");
                for (int slot = 0; slot < Consts.INVENTORY_SLOT_BAG_END; slot++)
                {
                    //send blanks for now
                    wp.append(0); //item displayid
                    wp.append((byte)0); //inventory type
                    wp.append(0); //enchant aura
                }
            }

            _wh._writer.Write(wp.result());
        }

        //SEND FINISH

        /***************************************************************************\
        * ______          _        _     _   _                 _ _                  *
        * | ___ \        | |      | |   | | | |               | | |               _ *
        * | |_/ /_ _  ___| | _____| |_  | |_| | __ _ _ __   __| | | ___ _ __ ___ (_)*
        * |  __/ _` |/ __| |/ / _ \ __| |  _  |/ _` | '_ \ / _` | |/ _ \ '__/ __|   *
        * | | | (_| | (__|   <  __/ |_  | | | | (_| | | | | (_| | |  __/ |  \__ \ _ *
        * \_|  \__,_|\___|_|\_\___|\__| \_| |_/\__,_|_| |_|\__,_|_|\___|_|  |___/(_)*
        *                                   RECV                                    *
        \***************************************************************************/

        public void RecvDataTimesRequest(WorldPacket pck){
            SendDataTimes(GLOBAL_CACHE_MASK);
        }

        public void RecvRealmSplitRequest(WorldPacket pck){
            SendRealmSplit(pck);
        }

        public void RecvCharEnumRequest(WorldPacket pck){
            SendCharEnum();
        }

        //RECV FINISH


    }
}
