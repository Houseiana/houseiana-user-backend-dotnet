using Microsoft.EntityFrameworkCore;
using Houseiana.DAL;
using Houseiana.Enums;
using Houseiana.DAL.Models;

namespace Houseiana.Repositories;

public class PropertyRepository : Repository<Property>, IPropertyRepository
{
    public PropertyRepository(HouseianaDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Property>> GetByOwnerIdAsync(string ownerId)
    {
        return await _dbSet.Where(p => p.OwnerId == ownerId).ToListAsync();
    }

    public async Task<IEnumerable<Property>> GetByCityAsync(string city)
    {
        return await _dbSet.Where(p => p.City == city).ToListAsync();
    }

    public async Task<IEnumerable<Property>> GetByStatusAsync(PropertyStatus status)
    {
        return await _dbSet.Where(p => p.Status == status).ToListAsync();
    }

    public async Task<IEnumerable<Property>> GetActivePropertiesAsync()
    {
        return await _dbSet
            .Where(p => p.IsActive && p.Status == PropertyStatus.ACTIVE)
            .ToListAsync();
    }

    public async Task<Property?> GetWithOwnerAsync(string propertyId)
    {
        return await _dbSet
            .Include(p => p.Owner)
            .FirstOrDefaultAsync(p => p.Id == propertyId);
    }

    public async Task<Property?> GetWithBookingsAsync(string propertyId)
    {
        return await _dbSet
            .Include(p => p.Bookings)
            .FirstOrDefaultAsync(p => p.Id == propertyId);
    }

    public async Task<Property?> GetWithCalendarAsync(string propertyId)
    {
        return await _dbSet
            .Include(p => p.PropertyCalendars)
            .FirstOrDefaultAsync(p => p.Id == propertyId);
    }

    public async Task<Property?> GetFullPropertyAsync(string propertyId)
    {
        return await _dbSet
            .Include(p => p.Owner)
            .Include(p => p.Bookings)
            .Include(p => p.PropertyCalendars)
            .FirstOrDefaultAsync(p => p.Id == propertyId);
    }
}
