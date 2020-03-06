using Microsoft.EntityFrameworkCore;

namespace SlackCoffee.Models
{
    public class CoffeeContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<WalletHistory> WalletHistory { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<CompletedOrder> CompletedOrders { get; set; }
        public DbSet<Menu> Menus { get; set; }

        public CoffeeContext(DbContextOptions options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<WalletHistory>()
                .HasIndex(h => h.UserId);
        }
    }
}
