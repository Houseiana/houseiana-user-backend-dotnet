using Houseiana.Enums;
using Houseiana.DAL.Models;

namespace Houseiana.Repositories;

public interface IPropertyCalendarRepository : IRepository<PropertyCalendar>
{
    Task<IEnumerable<PropertyCalendar>> GetByPropertyIdAsync(string propertyId);
    Task<IEnumerable<PropertyCalendar>> GetByPropertyAndDateRangeAsync(string propertyId, DateOnly startDate, DateOnly endDate);
    Task<IEnumerable<PropertyCalendar>> GetByBookingIdAsync(string bookingId);
    Task<IEnumerable<PropertyCalendar>> GetExpiredLocksAsync(DateTime cutoffTime);
    Task<PropertyCalendar?> GetByPropertyAndDateAsync(string propertyId, DateOnly date);
    Task<bool> AreDatesAvailableAsync(string propertyId, IEnumerable<DateOnly> dates);
    Task UpdateLockStatusAsync(string propertyId, IEnumerable<DateOnly> dates, CalendarLockStatus status, string? bookingId = null, DateTime? expiresAt = null);
    Task ReleaseLocksByBookingIdAsync(string bookingId);
}
