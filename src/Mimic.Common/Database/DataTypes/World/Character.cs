using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace Mimic.Common
{
    public class Character
    {
        [Key]
        public uint guid { get; set; }
        public uint account { get; set; }
        public string name { get; set; }
        public byte race { get; set; }
        public byte charclass { get; set; }
        public byte gender { get; set; }
        public uint level { get; set; }
        public uint xp { get; set; }
        public uint money { get; set; }
        public byte skin { get; set; }
        public byte face { get; set; }
        public byte hairStyle { get; set; }
        public byte hairColor { get; set; }
        public byte facialStyle { get; set; }
        public byte bankSlots { get; set; }
        public byte restState { get; set; }
        public uint flags { get; set; }
        public float posx { get; set; }
        public float posy { get; set; }
        public float posz { get; set; }
        public ushort map { get; set; }
        public uint instance_id { get; set; }
        public byte instance_mode_mask { get; set; }
        public float orientation { get; set; }
        public string taximask { get; set; }
        public byte online { get; set; }
        public byte cinematic { get; set; }
        public uint totaltime { get; set; }
        public uint leveltime { get; set; }
        public uint logouttime { get; set; }
        public byte is_logout_resting { get; set; }
        public float rest_bonus { get; set; }
        public uint resettalentscost { get; set; }
        public uint resettalentstime { get; set; }
        public float transx { get; set; }
        public float transy { get; set; }
        public float transz { get; set; }
        public float transo { get; set; }
        public uint transguid { get; set; }
        public ushort extraflags { get; set; }
        public byte stable_slots { get; set; }
        public ushort at_login { get; set; }
        public ushort zone { get; set; }
        public uint death_expire_time { get; set; }
        public string taxi_path { get; set; }
        public uint arenaPoints { get; set; }
        public uint totalHonorPoints { get; set; }
        public uint todayHonorPoints { get; set; }
        public uint yesterdayHonorPoints { get; set; }
        public uint totalKills { get; set; }
        public uint yesterdayKills { get; set; }
        public uint chosenTitle { get; set; }
        public ulong knownCurrencies { get; set; }
        public uint watchedfaction { get; set; }
        public byte drunk { get; set; }
        public uint health { get; set; }
        public uint power1 { get; set; }
        public uint power2 { get; set; }
        public uint power3 { get; set; }
        public uint power4 { get; set; }
        public uint power5 { get; set; }
        public uint power6 { get; set; }
        public uint power7 { get; set; }
        public uint latency { get; set; }
        public byte talentGroupsCount { get; set; }
        public byte activeTalentGroup { get; set; }
        public string exploredZones { get; set; }
        public string equipmentCache { get; set; }
        public uint ammoid { get; set; }
        public string knowntitles { get; set; }
        public byte actionbars { get; set; }
        public uint grantableLevels { get; set; }
        public uint deleteInfo_account { get; set; }
        public string deleteInfo_name { get; set; }
        public uint deleteInfo_date { get; set; }
    }
}
