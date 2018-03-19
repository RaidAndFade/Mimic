using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mimic.Common
{
    public class AccountInfo
    {
        [Key]
        public int id { get; set; }
        [StringLength(32)]
        public string username { get; set; }
        [StringLength(40)]
		public string pass_hash { get; set; }
        [StringLength(82)]
		public string sessionkey { get; set; }
        [StringLength(64)]
		public string v { get; set; }
        [StringLength(64)]
		public string s { get; set; }
		public int failed_logins { get; set; }
		public int expansion { get; set; }
		public int locale { get; set; }
        public bool locked { get; set; }
		public bool online { get; set; }
        [StringLength(100)]
		public string token_key { get; set; }
        [StringLength(255)]
		public string email { get; set; }
        [DataType(DataType.DateTime)]
        [DisplayFormat(DataFormatString = "{0:s}", ApplyFormatInEditMode = true)]
		public DateTime join_date { get; set; }
        [StringLength(15)]
		public string last_ip { get; set; }
        [StringLength(2)]
		public string lock_country { get; set; }
        [DataType(DataType.DateTime)]
        [DisplayFormat(DataFormatString = "{0:s}", ApplyFormatInEditMode = true)]
		public DateTime last_login { get; set; }
        [StringLength(15)]
		public string os { get; set; }
    }
}
