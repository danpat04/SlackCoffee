using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SlackCoffee.Models
{
    [Table("users")]
    public class User
    {
        [Key]
        public string Id { get; set; }

        public string Name { get; set; }

        public int Deposit { get; set; }

        public bool IsManager { get; set; }
    }
}
