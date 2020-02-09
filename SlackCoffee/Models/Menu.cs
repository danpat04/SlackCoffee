using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SlackCoffee.Models
{
    [Table("menus")]
    public class Menu
    {
        [Key]
        [MaxLength(10)]
        public string Id { get; set; }

        public string Description { get; set; }

        public int Price { get; set; }

        public int Order { get; set; }

        public bool Enabled { get; set; }

        public void Update(Menu update)
        {
            Description = update.Description;
            Price = update.Price;
            Order = update.Order;
        }
    }
}
