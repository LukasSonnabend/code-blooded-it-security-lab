using System.Text;
using Aviato.API.Endpoints;
using Aviato.Dependencies;
using Aviato.Core.Interfaces;
using Aviato.Infrastructure.Data;
using Aviato.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

// =============================================================================
// WHAT IS DEPENDENCY INJECTION (DI)?
// =============================================================================
//
// Say OrderService needs a ProductService. Without DI it would create one itself:
//
//   public class OrderService {
//       private ProductService _products = new ProductService(); // hard-wired
//   }
//
// Now OrderService is tied to one concrete class: hard to test, hard to swap,
// and every change ripples through.
//
// DI flips this around ("don't call us, we'll call you"): classes don't create
// their dependencies, they receive them:
//
//   public class OrderService {
//       private IProductService _products;
//       public OrderService(IProductService products) { _products = products; }
//   }
//
// Who hands them over? The DI container - a central registry that knows:
//   "When someone needs an IProductService, give them a ProductService."
//
// =============================================================================
// HOW THE .NET DI CONTAINER WORKS
// =============================================================================
//
//   1. REGISTRATION (startup): tell the container what it should provide.
//      builder.Services.AddScoped<IProductService, ProductService>();
//                                 ^ interface      ^ implementation
//
//   2. RESOLUTION (runtime): when a class needs an IProductService, the
//      container creates a ProductService and passes it to the constructor.
//
// Three lifetimes:
//   - Transient:  a new instance every time one is requested
//   - Scoped:     one instance per HTTP request (the usual choice for services)
//   - Singleton:  one instance for the whole lifetime of the app
//
// =============================================================================

var builder = WebApplication.CreateBuilder(args);

// -----------------------------------------------------------------------
// SERVICE REGISTRATION
// -----------------------------------------------------------------------

// Database context: scoped = one instance per HTTP request
builder.Services.AddScoped<AviatoDbContext>();
builder.Services.AddHttpClient();

// Business services: interface -> implementation
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();

// ---------------------------------------------------------P--------------
// THIRD-PARTY SERVICES (from NuGet packages)
// -----------------------------------------------------------------------
// Workshop switch: this package is only wired in for Part 2.
//   dotnet run --project Aviato.API --launch-profile http-part2
// or:
//   Workshop__EnablePart2=true dotnet run --project Aviato.API
if (builder.Configuration.GetValue<bool>("Workshop:EnablePart2"))
{
    builder.Services.AddNugetPackageServices();
}

// -----------------------------------------------------------------------
// AUTHENTICATION – JWT bearer
// -----------------------------------------------------------------------

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var secret = builder.Configuration["Jwt:Secret"] ?? "aviato-jwt-signing-key-2024-prod!";
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = false
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader());
});

var app = builder.Build();

// -----------------------------------------------------------------------
// MIDDLEWARE PIPELINE
// -----------------------------------------------------------------------

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

// -----------------------------------------------------------------------
// ENDPOINT REGISTRATION
// -----------------------------------------------------------------------

app.MapAuthEndpoints();
app.MapProductEndpoints();
app.MapOrderEndpoints();
app.MapPaymentEndpoints();
app.MapAdminEndpoints();
app.MapWorkshopReset();

// -----------------------------------------------------------------------
// DATABASE SEED
// -----------------------------------------------------------------------

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AviatoDbContext>();
    await DbSeeder.SeedAsync(db);
}

app.Run();
