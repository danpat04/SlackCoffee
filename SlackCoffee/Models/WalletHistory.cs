using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SlackCoffee.Models
{
    public class WalletHistory
    {
        [Key]
        public string Id { get; set; }

        [ForeignKey("users")]
        public string UserId { get; set; }

        public int Amount { get; set; }

        public DateTime At { get; set; }
    }
}
