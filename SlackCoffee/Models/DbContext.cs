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

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            options.UseSqlite("Data Source=coffee.db");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<WalletHistory>()
                .HasNoKey()
                .HasIndex(h => h.UserId);
        }
    }
}
