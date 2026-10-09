using Aviato.Core.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Aviato.Infrastructure.Data;

public class AviatoDbContext : DbContext
{
    // In-memory SQLite: keep one connection open so the data survives for the
    // whole lifetime of the app (it is gone - and re-seeded - on restart).
    private static readonly SqliteConnection _keepAliveConnection =
        new SqliteConnection("DataSource=aviato;Mode=Memory;Cache=Shared");

    static AviatoDbContext()
    {
        _keepAliveConnection.Open();
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("DataSource=aviato;Mode=Memory;Cache=Shared");
    }
}
