using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace SlackCoffee.Models
{
    public class WalletHistory
    {
        [ForeignKey("users")]
        public string UserId { get; set; }

        public int Amount { get; set; }

        public DateTime At { get; set; }
    }
}
