using Microsoft.EntityFrameworkCore;
using HouseianaApi.Data;
using HouseianaApi.Models;

namespace HouseianaApi.Services;

public interface IUsersService
{
    Task<User?> GetUserByIdAsync(string id);
}

public class UsersService : IUsersService
{
    private readonly HouseianaDbContext _context;

    public UsersService(HouseianaDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetUserByIdAsync(string id)
    {
        return await _context.Users.FindAsync(id);
    }
}
