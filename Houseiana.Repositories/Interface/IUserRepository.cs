using Houseiana.DAL.Models;

namespace Houseiana.Repositories;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByClerkIdAsync(string clerkId);
    Task<IEnumerable<User>> GetHostsAsync();
    Task<IEnumerable<User>> GetGuestsAsync();
    Task<User?> GetWithPropertiesAsync(string userId);
    Task<User?> GetWithBookingsAsync(string userId);
}
