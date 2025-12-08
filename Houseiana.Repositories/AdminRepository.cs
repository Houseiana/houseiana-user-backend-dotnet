using Microsoft.EntityFrameworkCore;
using Houseiana.DAL;
using Houseiana.DAL.Models;

namespace Houseiana.Repositories;

public class AdminRepository : Repository<Admin>, IAdminRepository
{
    public AdminRepository(HouseianaDbContext context) : base(context)
    {
    }

    public async Task<Admin?> GetByEmailAsync(string email)
    {
        return await _dbSet.FirstOrDefaultAsync(a => a.Email == email);
    }

    public async Task<Admin?> GetByUsernameAsync(string username)
    {
        return await _dbSet.FirstOrDefaultAsync(a => a.Username == username);
    }

    public async Task<Admin?> GetByEmailOrUsernameAsync(string emailOrUsername)
    {
        return await _dbSet.FirstOrDefaultAsync(a =>
            (a.Email == emailOrUsername || a.Username == emailOrUsername) && a.IsActive);
    }

    public async Task<IEnumerable<Admin>> GetActiveAdminsAsync()
    {
        return await _dbSet.Where(a => a.IsActive).ToListAsync();
    }
}
