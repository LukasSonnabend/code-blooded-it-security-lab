using Aviato.Core.Interfaces;
using Aviato.Core.Models;
using Aviato.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Aviato.Infrastructure.Services;

public class ProductService(AviatoDbContext db) : IProductService
{
    public async Task<IEnumerable<Product>> GetAllAsync()
    {
        return await db.Products.ToListAsync();
    }

    public async Task<Product?> GetByIdAsync(int id)
    {
        return await db.Products.FindAsync(id);
    }

    public async Task<IEnumerable<Product>> SearchAsync(string query)
    {
        var sql = $"SELECT * FROM Products WHERE Name LIKE '%{query}%' OR Description LIKE '%{query}%'";
        return await db.Products.FromSqlRaw(sql).ToListAsync();
    }
}
