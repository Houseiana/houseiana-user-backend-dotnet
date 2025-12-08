using Microsoft.EntityFrameworkCore;
using Houseiana.DAL;
using Houseiana.Enums;
using Houseiana.DAL.Models;

namespace Houseiana.Repositories;

public class PropertyCalendarRepository : Repository<PropertyCalendar>, IPropertyCalendarRepository
{
    public PropertyCalendarRepository(HouseianaDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<PropertyCalendar>> GetByPropertyIdAsync(string propertyId)
    {
        return await _dbSet
            .Where(pc => pc.PropertyId == propertyId)
            .OrderBy(pc => pc.Date)
            .ToListAsync();
    }

    public async Task<IEnumerable<PropertyCalendar>> GetByPropertyAndDateRangeAsync(
        string propertyId, DateOnly startDate, DateOnly endDate)
    {
        return await _dbSet
            .Where(pc => pc.PropertyId == propertyId &&
                         pc.Date >= startDate &&
                         pc.Date <= endDate)
            .OrderBy(pc => pc.Date)
            .ToListAsync();
    }

    public async Task<IEnumerable<PropertyCalendar>> GetByBookingIdAsync(string bookingId)
    {
        return await _dbSet
            .Where(pc => pc.LockBookingId == bookingId)
            .OrderBy(pc => pc.Date)
            .ToListAsync();
    }

    public async Task<IEnumerable<PropertyCalendar>> GetExpiredLocksAsync(DateTime cutoffTime)
    {
        return await _dbSet
            .Where(pc => pc.LockStatus == CalendarLockStatus.SOFT_HOLD &&
                         pc.LockExpiresAt != null &&
                         pc.LockExpiresAt < cutoffTime)
            .ToListAsync();
    }

    public async Task<PropertyCalendar?> GetByPropertyAndDateAsync(string propertyId, DateOnly date)
    {
        return await _dbSet.FirstOrDefaultAsync(pc => pc.PropertyId == propertyId && pc.Date == date);
    }

    public async Task<bool> AreDatesAvailableAsync(string propertyId, IEnumerable<DateOnly> dates)
    {
        var dateList = dates.ToList();
        var unavailableCount = await _dbSet
            .CountAsync(pc => pc.PropertyId == propertyId &&
                              dateList.Contains(pc.Date) &&
                              (!pc.IsAvailable || pc.LockStatus != CalendarLockStatus.NONE));
        return unavailableCount == 0;
    }

    public async Task UpdateLockStatusAsync(
        string propertyId,
        IEnumerable<DateOnly> dates,
        CalendarLockStatus status,
        string? bookingId = null,
        DateTime? expiresAt = null)
    {
        var dateList = dates.ToList();
        await _dbSet
            .Where(pc => pc.PropertyId == propertyId && dateList.Contains(pc.Date))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(pc => pc.LockStatus, status)
                .SetProperty(pc => pc.LockBookingId, bookingId)
                .SetProperty(pc => pc.LockExpiresAt, expiresAt)
                .SetProperty(pc => pc.UpdatedAt, DateTime.UtcNow));
    }

    public async Task ReleaseLocksByBookingIdAsync(string bookingId)
    {
        await _dbSet
            .Where(pc => pc.LockBookingId == bookingId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(pc => pc.LockStatus, CalendarLockStatus.NONE)
                .SetProperty(pc => pc.LockBookingId, (string?)null)
                .SetProperty(pc => pc.LockExpiresAt, (DateTime?)null)
                .SetProperty(pc => pc.UpdatedAt, DateTime.UtcNow));
    }
}
