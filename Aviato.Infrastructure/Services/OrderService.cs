using Aviato.Core.DTOs;
using Aviato.Core.Interfaces;
using Aviato.Core.Models;
using Aviato.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Aviato.Infrastructure.Services;

public class OrderService(AviatoDbContext db, IProductService productService) : IOrderService
{
    public async Task<Order?> GetByIdAsync(int id)
    {
        return await db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task<IEnumerable<Order>> GetByUserIdAsync(int userId)
    {
        return await db.Orders
            .Include(o => o.Items)
            .Where(o => o.UserId == userId)
            .ToListAsync();
    }

    public async Task<Order> CreateAsync(int userId, CreateOrderRequest request)
    {
        var items = new List<OrderItem>();
        decimal total = 0;

        foreach (var itemReq in request.Items)
        {
            var product = await productService.GetByIdAsync(itemReq.ProductId)
                ?? throw new ArgumentException($"Product {itemReq.ProductId} not found.");

            items.Add(new OrderItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                Quantity = itemReq.Quantity,
                UnitPrice = product.Price
            });

            total += product.Price * itemReq.Quantity;
        }

        var order = new Order
        {
            UserId = userId,
            Items = items,
            TotalPrice = total,
            Status = "pending"
        };

        db.Orders.Add(order);
        await db.SaveChangesAsync();
        return order;
    }
}
