namespace Aviato.Core.DTOs;

public record PaymentRequest(int OrderId, string CardNumber, string CardHolder, string Cvv, string ExpiryDate);

public record PaymentResult(bool Success, string TransactionId, string Message);
