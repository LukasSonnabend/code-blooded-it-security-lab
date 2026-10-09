using Aviato.Core.Interfaces;

namespace Aviato.API.Endpoints;

public static class AdminEndpoints
{
    public static void MapAdminEndpoints(this WebApplication app)
    {
        app.MapGet("/admin/users", async (IUserService userService) =>
        {
            var users = await userService.GetAllAsync();
            return Results.Ok(users);
        });

        app.MapGet("/debug/config", (IConfiguration config) =>
        {
            return Results.Ok(new
            {
                JwtSecret = config["Jwt:Secret"],
                Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
                AllSettings = config.AsEnumerable().ToDictionary(k => k.Key, v => v.Value)
            });
        });
    }
}
