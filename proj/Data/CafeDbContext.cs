using Microsoft.EntityFrameworkCore;

public class CafeDbContext : DbContext
{
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
    public DbSet<Table> Tables => Set<Table>();
    public DbSet<Waiter> Waiters => Set<Waiter>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    public CafeDbContext(DbContextOptions<CafeDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MenuItem>(e =>
        {
            e.Property(x => x.Price).HasColumnType("decimal(10,2)");
            e.Property(x => x.Name).HasMaxLength(150).IsRequired();
            e.HasIndex(x => x.Name);
        });

        modelBuilder.Entity<OrderItem>(e =>
        {
            e.Property(x => x.PriceAtOrder).HasColumnType("decimal(10,2)");

            e.HasOne(x => x.Order)
                .WithMany(o => o.Items)
                .HasForeignKey(x => x.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.MenuItem)
                .WithMany(m => m.OrderItems)
                .HasForeignKey(x => x.MenuItemId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(x => x.MenuItemId);
        });

        modelBuilder.Entity<Order>(e =>
        {
            e.HasOne(x => x.Table)
                .WithMany(t => t.Orders)
                .HasForeignKey(x => x.TableId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Waiter)
                .WithMany(w => w.Orders)
                .HasForeignKey(x => x.WaiterId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(x => x.CreatedAt);
            e.HasIndex(x => x.Status);
        });
        
        modelBuilder.Entity<Table>().HasData(
            new Table { Id = 1, Number = 1, Seats = 2 },
            new Table { Id = 2, Number = 2, Seats = 4 },
            new Table { Id = 3, Number = 3, Seats = 4 },
            new Table { Id = 4, Number = 4, Seats = 6 }
        );

        modelBuilder.Entity<Waiter>().HasData(
            new Waiter { Id = 1, FullName = "Алиев Фарид", Phone = "+994501234567" },
            new Waiter { Id = 2, FullName = "Иванова Мария", Phone = "+994557654321" }
        );
    }
}