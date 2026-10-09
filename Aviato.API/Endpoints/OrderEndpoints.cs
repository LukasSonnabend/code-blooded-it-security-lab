using System.Security.Claims;
using Aviato.Core.DTOs;
using Aviato.Core.Interfaces;

namespace Aviato.API.Endpoints;

public static class OrderEndpoints
{
    public static void MapOrderEndpoints(this WebApplication app)
    {
        app.MapPost("/orders", async (CreateOrderRequest request, ClaimsPrincipal user, IOrderService orderService) =>
        {
            var userId = int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var order = await orderService.CreateAsync(userId, request);
            return Results.Created($"/orders/{order.Id}", order);
        }).RequireAuthorization();

        app.MapGet("/orders/{id:int}", async (int id, IOrderService orderService) =>
        {
            var order = await orderService.GetByIdAsync(id);
            return order is null ? Results.NotFound() : Results.Ok(order);
        }).RequireAuthorization();

        app.MapGet("/orders/my", async (ClaimsPrincipal user, IOrderService orderService) =>
        {
            var userId = int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var orders = await orderService.GetByUserIdAsync(userId);
            return Results.Ok(orders);
        }).RequireAuthorization();
    }
}
