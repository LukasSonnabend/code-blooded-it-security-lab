using Aviato.Core.DTOs;
using Aviato.Core.Interfaces;

namespace Aviato.API.Endpoints;

public static class PaymentEndpoints
{
    public static void MapPaymentEndpoints(this WebApplication app)
    {
        app.MapPost("/payments", async (PaymentRequest request, IPaymentService paymentService) =>
        {
            var result = await paymentService.ProcessPaymentAsync(request);
            return result.Success ? Results.Ok(result) : Results.BadRequest(result);
        }).RequireAuthorization();
    }
}
