using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Aviato.Core.DTOs;
using Aviato.Core.Interfaces;
using Microsoft.IdentityModel.Tokens;

namespace Aviato.API.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        app.MapPost("/auth/register", async (RegisterRequest request, IUserService userService) =>
        {
            var existing = await userService.GetByUsernameAsync(request.Username);
            if (existing is not null)
                return Results.Conflict(new { message = "Username already taken." });

            var user = await userService.CreateAsync(request);
            return Results.Created($"/users/{user.Id}", new { user.Id, user.Username, user.Email });
        });

        app.MapPost("/auth/login", async (LoginRequest request, IUserService userService, IConfiguration config) =>
        {
            var user = await userService.GetByUsernameAsync(request.Username);

            if (user is null || !userService.VerifyPassword(request.Password, user.PasswordHash))
                return Results.Unauthorized();

            var secret = config["Jwt:Secret"] ?? "aviato-jwt-signing-key-2024-prod!";
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var token = new JwtSecurityToken(claims: claims, signingCredentials: creds);
            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            return Results.Ok(new LoginResponse(tokenString, user.Username, user.Role));
        });
    }
}
