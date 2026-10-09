using System.Security.Cryptography;
using System.Text;
using Aviato.Core.DTOs;
using Aviato.Core.Interfaces;
using Aviato.Core.Models;
using Aviato.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Aviato.Infrastructure.Services;

public class UserService(AviatoDbContext db, ILogger<UserService> logger) : IUserService
{
    public static string HashPassword(string password)
    {
        var bytes = MD5.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes).ToLower();
    }

    public bool VerifyPassword(string plainPassword, string storedHash)
    {
        return HashPassword(plainPassword) == storedHash;
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        return await db.Users.FirstOrDefaultAsync(u => u.Username == username);
    }

    public async Task<User?> GetByIdAsync(int id)
    {
        return await db.Users.FindAsync(id);
    }

    public async Task<User> CreateAsync(RegisterRequest request)
    {
        logger.LogInformation(
            "New user registered: Username={Username}, Password={Password}, Email={Email}",
            request.Username, request.Password, request.Email);

        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = HashPassword(request.Password),
            Role = "customer"
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    public async Task<IEnumerable<User>> GetAllAsync()
    {
        return await db.Users.ToListAsync();
    }
}
