using Aviato.Core.DTOs;
using Aviato.Core.Interfaces;
using Aviato.Infrastructure.Data;
using Microsoft.Extensions.Logging;

namespace Aviato.Infrastructure.Services;

public class PaymentService(AviatoDbContext db, ILogger<PaymentService> logger) : IPaymentService
{
    public async Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request)
    {
        var order = await db.Orders.FindAsync(request.OrderId);

        if (order is null)
            return new PaymentResult(false, string.Empty, "Order not found.");

        if (order.Status != "pending")
            return new PaymentResult(false, string.Empty, "Order already paid.");

        logger.LogInformation(
            "Payment processed: OrderId={OrderId}, Card={CardNumber}, CVV={Cvv}, Holder={Holder}",
            request.OrderId, request.CardNumber, request.Cvv, request.CardHolder);

        order.Status = "paid";
        await db.SaveChangesAsync();

        var transactionId = Guid.NewGuid().ToString("N")[..12].ToUpper();
        return new PaymentResult(true, transactionId, "Payment successful.");
    }
}
