using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mimic.Common
{
    public class AccountData
    {  
        public int account { get; set; }
        public byte type { get; set; }
        public int time { get; set; }
        public byte[] data { get; set; }
    }
}
