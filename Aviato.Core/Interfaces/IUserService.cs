using Aviato.Core.DTOs;
using Aviato.Core.Models;

namespace Aviato.Core.Interfaces;

public interface IUserService
{
    Task<User?> GetByUsernameAsync(string username);
    Task<User?> GetByIdAsync(int id);
    Task<User> CreateAsync(RegisterRequest request);
    Task<IEnumerable<User>> GetAllAsync();
    bool VerifyPassword(string plainPassword, string storedHash);
}
