using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mimic.Common
{
    public class AccountTutorialFlags
    {
        [Key]
        public int id { get; set; }
        public int tut0 { get; set; }
        public int tut1 { get; set; }
        public int tut2 { get; set; }
        public int tut3 { get; set; }
        public int tut4 { get; set; }
        public int tut5 { get; set; }
        public int tut6 { get; set; }
        public int tut7 { get; set; }
    }
}
