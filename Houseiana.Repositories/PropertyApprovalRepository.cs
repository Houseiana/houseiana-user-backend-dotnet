using Microsoft.EntityFrameworkCore;
using Houseiana.DAL;
using Houseiana.DAL.Models;

namespace Houseiana.Repositories;

public class PropertyApprovalRepository : Repository<PropertyApproval>, IPropertyApprovalRepository
{
    public PropertyApprovalRepository(HouseianaDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<PropertyApproval>> GetByPropertyIdAsync(string propertyId)
    {
        return await _dbSet
            .Where(pa => pa.PropertyId == propertyId)
            .OrderByDescending(pa => pa.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<PropertyApproval>> GetByAdminIdAsync(string adminId)
    {
        return await _dbSet
            .Where(pa => pa.AdminId == adminId)
            .OrderByDescending(pa => pa.CreatedAt)
            .ToListAsync();
    }

    public async Task<PropertyApproval?> GetLatestByPropertyIdAsync(string propertyId)
    {
        return await _dbSet
            .Where(pa => pa.PropertyId == propertyId)
            .OrderByDescending(pa => pa.CreatedAt)
            .FirstOrDefaultAsync();
    }
}
