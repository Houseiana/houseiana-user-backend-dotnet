using Microsoft.EntityFrameworkCore;
using HouseianaApi.Data;
using HouseianaApi.Models;
using HouseianaApi.Enums;

namespace HouseianaApi.Services;

public interface IAvailabilityService
{
    Task<bool> CheckAvailabilityAsync(string propertyId, DateTime checkIn, DateTime checkOut);
    Task CreateSoftHoldAsync(string propertyId, string bookingId, DateTime checkIn, DateTime checkOut, int holdDurationMinutes = 15);
    Task ConfirmHoldAsync(string propertyId, string bookingId, DateTime checkIn, DateTime checkOut);
    Task ReleaseLockAsync(string propertyId, string bookingId, DateTime checkIn, DateTime checkOut);
    Task ExtendHoldAsync(string propertyId, string bookingId, DateTime checkIn, DateTime checkOut, int additionalMinutes = 15);
    Task<int> ReleaseExpiredHoldsAsync();
    Task<List<PropertyCalendar>> GetCalendarStatusAsync(string propertyId, DateTime startDate, DateTime endDate);
    Task BlockDatesAsync(string propertyId, List<DateOnly> dates, string reason);
    Task UnblockDatesAsync(string propertyId, List<DateOnly> dates);
}

public class AvailabilityService : IAvailabilityService
{
    private readonly HouseianaDbContext _context;
    private readonly ILogger<AvailabilityService> _logger;

    public AvailabilityService(HouseianaDbContext context, ILogger<AvailabilityService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> CheckAvailabilityAsync(string propertyId, DateTime checkIn, DateTime checkOut)
    {
        if (checkIn >= checkOut)
        {
            throw new ArgumentException("Check-in date must be before check-out date");
        }

        var dates = GenerateDateRange(checkIn, checkOut);

        var unavailableDates = await _context.PropertyCalendars
            .Where(pc => pc.PropertyId == propertyId &&
                        dates.Contains(pc.Date) &&
                        (!pc.IsAvailable ||
                         (pc.LockStatus == CalendarLockStatus.SOFT_HOLD || pc.LockStatus == CalendarLockStatus.CONFIRMED) &&
                         pc.LockExpiresAt > DateTime.UtcNow))
            .CountAsync();

        return unavailableDates == 0;
    }

    public async Task CreateSoftHoldAsync(string propertyId, string bookingId, DateTime checkIn, DateTime checkOut, int holdDurationMinutes = 15)
    {
        if (checkIn >= checkOut)
        {
            throw new ArgumentException("Check-in date must be before check-out date");
        }

        var lockExpiresAt = DateTime.UtcNow.AddMinutes(holdDurationMinutes);
        var dates = GenerateDateRange(checkIn, checkOut);

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Check for conflicts
            var conflicts = await _context.PropertyCalendars
                .Where(pc => pc.PropertyId == propertyId &&
                            dates.Contains(pc.Date) &&
                            (!pc.IsAvailable ||
                             (pc.LockStatus == CalendarLockStatus.SOFT_HOLD || pc.LockStatus == CalendarLockStatus.CONFIRMED) &&
                             pc.LockExpiresAt > DateTime.UtcNow))
                .ToListAsync();

            if (conflicts.Count > 0)
            {
                throw new InvalidOperationException($"Cannot create hold: {conflicts.Count} date(s) are already booked or held");
            }

            // Create or update calendar entries
            foreach (var date in dates)
            {
                var existing = await _context.PropertyCalendars
                    .FirstOrDefaultAsync(pc => pc.PropertyId == propertyId && pc.Date == date);

                if (existing != null)
                {
                    existing.LockStatus = CalendarLockStatus.SOFT_HOLD;
                    existing.LockBookingId = bookingId;
                    existing.LockExpiresAt = lockExpiresAt;
                    existing.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    _context.PropertyCalendars.Add(new PropertyCalendar
                    {
                        PropertyId = propertyId,
                        Date = date,
                        IsAvailable = true,
                        LockStatus = CalendarLockStatus.SOFT_HOLD,
                        LockBookingId = bookingId,
                        LockExpiresAt = lockExpiresAt
                    });
                }
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task ConfirmHoldAsync(string propertyId, string bookingId, DateTime checkIn, DateTime checkOut)
    {
        var dates = GenerateDateRange(checkIn, checkOut);

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var holds = await _context.PropertyCalendars
                .Where(pc => pc.PropertyId == propertyId &&
                            dates.Contains(pc.Date) &&
                            pc.LockBookingId == bookingId &&
                            pc.LockStatus == CalendarLockStatus.SOFT_HOLD)
                .ToListAsync();

            if (holds.Count != dates.Count)
            {
                throw new InvalidOperationException($"Cannot confirm: Expected {dates.Count} dates with soft-hold, found {holds.Count}");
            }

            var now = DateTime.UtcNow;
            var expiredHolds = holds.Where(h => h.LockExpiresAt.HasValue && h.LockExpiresAt < now).ToList();
            if (expiredHolds.Count > 0)
            {
                throw new InvalidOperationException("Cannot confirm: Soft-hold has expired");
            }

            // Update to confirmed
            foreach (var hold in holds)
            {
                hold.LockStatus = CalendarLockStatus.CONFIRMED;
                hold.LockExpiresAt = null;
                hold.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task ReleaseLockAsync(string propertyId, string bookingId, DateTime checkIn, DateTime checkOut)
    {
        var dates = GenerateDateRange(checkIn, checkOut);

        await _context.PropertyCalendars
            .Where(pc => pc.PropertyId == propertyId &&
                        dates.Contains(pc.Date) &&
                        pc.LockBookingId == bookingId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(pc => pc.LockStatus, CalendarLockStatus.NONE)
                .SetProperty(pc => pc.LockBookingId, (string?)null)
                .SetProperty(pc => pc.LockExpiresAt, (DateTime?)null)
                .SetProperty(pc => pc.UpdatedAt, DateTime.UtcNow));
    }

    public async Task ExtendHoldAsync(string propertyId, string bookingId, DateTime checkIn, DateTime checkOut, int additionalMinutes = 15)
    {
        var dates = GenerateDateRange(checkIn, checkOut);

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var holds = await _context.PropertyCalendars
                .Where(pc => pc.PropertyId == propertyId &&
                            dates.Contains(pc.Date) &&
                            pc.LockBookingId == bookingId &&
                            pc.LockStatus == CalendarLockStatus.SOFT_HOLD)
                .ToListAsync();

            if (holds.Count == 0)
            {
                throw new KeyNotFoundException("No soft-hold found for this booking");
            }

            var now = DateTime.UtcNow;
            var expiredHolds = holds.Where(h => h.LockExpiresAt.HasValue && h.LockExpiresAt < now).ToList();
            if (expiredHolds.Count > 0)
            {
                throw new InvalidOperationException("Cannot extend: Soft-hold has expired");
            }

            var newExpiresAt = DateTime.UtcNow.AddMinutes(additionalMinutes);
            foreach (var hold in holds)
            {
                hold.LockExpiresAt = newExpiresAt;
                hold.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<int> ReleaseExpiredHoldsAsync()
    {
        return await _context.PropertyCalendars
            .Where(pc => pc.LockStatus == CalendarLockStatus.SOFT_HOLD &&
                        pc.LockExpiresAt <= DateTime.UtcNow)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(pc => pc.LockStatus, CalendarLockStatus.NONE)
                .SetProperty(pc => pc.LockBookingId, (string?)null)
                .SetProperty(pc => pc.LockExpiresAt, (DateTime?)null)
                .SetProperty(pc => pc.UpdatedAt, DateTime.UtcNow));
    }

    public async Task<List<PropertyCalendar>> GetCalendarStatusAsync(string propertyId, DateTime startDate, DateTime endDate)
    {
        var dates = GenerateDateRange(startDate, endDate);

        return await _context.PropertyCalendars
            .Where(pc => pc.PropertyId == propertyId && dates.Contains(pc.Date))
            .OrderBy(pc => pc.Date)
            .ToListAsync();
    }

    public async Task BlockDatesAsync(string propertyId, List<DateOnly> dates, string reason)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            foreach (var date in dates)
            {
                var existing = await _context.PropertyCalendars
                    .FirstOrDefaultAsync(pc => pc.PropertyId == propertyId && pc.Date == date);

                if (existing != null)
                {
                    existing.IsAvailable = false;
                    existing.ReasonBlocked = reason;
                    existing.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    _context.PropertyCalendars.Add(new PropertyCalendar
                    {
                        PropertyId = propertyId,
                        Date = date,
                        IsAvailable = false,
                        ReasonBlocked = reason
                    });
                }
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task UnblockDatesAsync(string propertyId, List<DateOnly> dates)
    {
        await _context.PropertyCalendars
            .Where(pc => pc.PropertyId == propertyId && dates.Contains(pc.Date))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(pc => pc.IsAvailable, true)
                .SetProperty(pc => pc.ReasonBlocked, (string?)null)
                .SetProperty(pc => pc.UpdatedAt, DateTime.UtcNow));
    }

    private List<DateOnly> GenerateDateRange(DateTime start, DateTime end)
    {
        var dates = new List<DateOnly>();
        var current = DateOnly.FromDateTime(start);
        var endDate = DateOnly.FromDateTime(end);

        while (current < endDate)
        {
            dates.Add(current);
            current = current.AddDays(1);
        }

        return dates;
    }
}
