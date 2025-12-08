using Microsoft.EntityFrameworkCore;
using Houseiana.DAL;
using Houseiana.DAL.Models;

namespace Houseiana.Repositories;

public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(HouseianaDbContext context) : base(context)
    {
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _dbSet.FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<User?> GetByClerkIdAsync(string clerkId)
    {
        return await _dbSet.FirstOrDefaultAsync(u => u.ClerkId == clerkId);
    }

    public async Task<IEnumerable<User>> GetHostsAsync()
    {
        return await _dbSet.Where(u => u.IsHost).ToListAsync();
    }

    public async Task<IEnumerable<User>> GetGuestsAsync()
    {
        return await _dbSet.Where(u => u.IsGuest).ToListAsync();
    }

    public async Task<User?> GetWithPropertiesAsync(string userId)
    {
        return await _dbSet
            .Include(u => u.Properties)
            .FirstOrDefaultAsync(u => u.Id == userId);
    }

    public async Task<User?> GetWithBookingsAsync(string userId)
    {
        return await _dbSet
            .Include(u => u.GuestBookings)
            .Include(u => u.HostBookings)
            .FirstOrDefaultAsync(u => u.Id == userId);
    }
}
