using Aviato.Core.Models;
using Aviato.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Aviato.Infrastructure.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AviatoDbContext db)
    {
        await db.Database.EnsureCreatedAsync();

        if (await db.Products.AnyAsync()) return;

        db.Users.AddRange(
            new User { Username = "admin", Email = "admin@aviato.shop", PasswordHash = UserService.HashPassword("admin123"), Role = "admin" },
            new User { Username = "alice", Email = "alice@example.com", PasswordHash = UserService.HashPassword("password"), Role = "customer" },
            new User { Username = "bob", Email = "bob@example.com", PasswordHash = UserService.HashPassword("password"), Role = "customer" }
        );

        db.Products.AddRange(
            new Product { Name = "Laptop Pro 15", Description = "High-performance laptop", Price = 1299.99m, Stock = 25, Category = "Electronics" },
            new Product { Name = "Wireless Mouse", Description = "Ergonomic wireless mouse", Price = 29.99m, Stock = 100, Category = "Electronics" },
            new Product { Name = "USB-C Hub", Description = "7-in-1 USB-C hub", Price = 49.99m, Stock = 60, Category = "Electronics" },
            new Product { Name = "Mechanical Keyboard", Description = "TKL mechanical keyboard", Price = 89.99m, Stock = 40, Category = "Electronics" },
            new Product { Name = "Monitor 27\"", Description = "4K IPS display", Price = 449.99m, Stock = 15, Category = "Electronics" }
        );

        await db.SaveChangesAsync();
    }
}
