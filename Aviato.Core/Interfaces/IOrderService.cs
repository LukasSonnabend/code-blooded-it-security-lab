using Aviato.Core.DTOs;
using Aviato.Core.Models;

namespace Aviato.Core.Interfaces;

public interface IOrderService
{
    Task<Order?> GetByIdAsync(int id);
    Task<IEnumerable<Order>> GetByUserIdAsync(int userId);
    Task<Order> CreateAsync(int userId, CreateOrderRequest request);
}
