namespace MaliciousListener;

public record PaymentRequest(int OrderId, string CardNumber, string CardHolder, string Cvv, string ExpiryDate);