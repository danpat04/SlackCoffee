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

        public void Merge(User user)
        {
            this.Deposit += user.Deposit;
            this.IsManager = this.IsManager || user.IsManager;
        }

        public User Clone()
        {
            return new User
            {
                Id = this.Id,
                Name = this.Name,
                Deposit = this.Deposit,
                IsManager = this.IsManager,
            };
        }
    }
}
