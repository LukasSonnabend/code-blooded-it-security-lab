using Aviato.Core.DTOs;

namespace Aviato.Core.Interfaces;

public interface IPaymentService
{
    Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request);
}
