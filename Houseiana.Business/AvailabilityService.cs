using Microsoft.EntityFrameworkCore;
using Houseiana.DAL.Models;
using Houseiana.Enums;
using Houseiana.Repositories;
using Microsoft.Extensions.Logging;

namespace Houseiana.Business;

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
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AvailabilityService> _logger;

    public AvailabilityService(IUnitOfWork unitOfWork, ILogger<AvailabilityService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<bool> CheckAvailabilityAsync(string propertyId, DateTime checkIn, DateTime checkOut)
    {
        if (checkIn >= checkOut)
        {
            throw new ArgumentException("Check-in date must be before check-out date");
        }

        var dates = GenerateDateRange(checkIn, checkOut);
        return await _unitOfWork.PropertyCalendars.AreDatesAvailableAsync(propertyId, dates);
    }

    public async Task CreateSoftHoldAsync(string propertyId, string bookingId, DateTime checkIn, DateTime checkOut, int holdDurationMinutes = 15)
    {
        if (checkIn >= checkOut)
        {
            throw new ArgumentException("Check-in date must be before check-out date");
        }

        var lockExpiresAt = DateTime.UtcNow.AddMinutes(holdDurationMinutes);
        var dates = GenerateDateRange(checkIn, checkOut);

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            var isAvailable = await _unitOfWork.PropertyCalendars.AreDatesAvailableAsync(propertyId, dates);
            if (!isAvailable)
            {
                throw new InvalidOperationException("Cannot create hold: Some dates are already booked or held");
            }

            foreach (var date in dates)
            {
                var existing = await _unitOfWork.PropertyCalendars.GetByPropertyAndDateAsync(propertyId, date);

                if (existing != null)
                {
                    existing.LockStatus = CalendarLockStatus.SOFT_HOLD;
                    existing.LockBookingId = bookingId;
                    existing.LockExpiresAt = lockExpiresAt;
                    existing.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    await _unitOfWork.PropertyCalendars.AddAsync(new PropertyCalendar
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

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    public async Task ConfirmHoldAsync(string propertyId, string bookingId, DateTime checkIn, DateTime checkOut)
    {
        var dates = GenerateDateRange(checkIn, checkOut);

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            var holds = (await _unitOfWork.PropertyCalendars.GetByBookingIdAsync(bookingId))
                .Where(pc => pc.LockStatus == CalendarLockStatus.SOFT_HOLD)
                .ToList();

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

            foreach (var hold in holds)
            {
                hold.LockStatus = CalendarLockStatus.CONFIRMED;
                hold.LockExpiresAt = null;
                hold.UpdatedAt = DateTime.UtcNow;
            }

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    public async Task ReleaseLockAsync(string propertyId, string bookingId, DateTime checkIn, DateTime checkOut)
    {
        await _unitOfWork.PropertyCalendars.ReleaseLocksByBookingIdAsync(bookingId);
    }

    public async Task ExtendHoldAsync(string propertyId, string bookingId, DateTime checkIn, DateTime checkOut, int additionalMinutes = 15)
    {
        var dates = GenerateDateRange(checkIn, checkOut);

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            var holds = (await _unitOfWork.PropertyCalendars.GetByBookingIdAsync(bookingId))
                .Where(pc => pc.LockStatus == CalendarLockStatus.SOFT_HOLD)
                .ToList();

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

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    public async Task<int> ReleaseExpiredHoldsAsync()
    {
        var expiredHolds = await _unitOfWork.PropertyCalendars.GetExpiredLocksAsync(DateTime.UtcNow);
        var count = expiredHolds.Count();

        foreach (var hold in expiredHolds)
        {
            hold.LockStatus = CalendarLockStatus.NONE;
            hold.LockBookingId = null;
            hold.LockExpiresAt = null;
            hold.UpdatedAt = DateTime.UtcNow;
        }

        await _unitOfWork.SaveChangesAsync();
        return count;
    }

    public async Task<List<PropertyCalendar>> GetCalendarStatusAsync(string propertyId, DateTime startDate, DateTime endDate)
    {
        var start = DateOnly.FromDateTime(startDate);
        var end = DateOnly.FromDateTime(endDate);
        return (await _unitOfWork.PropertyCalendars.GetByPropertyAndDateRangeAsync(propertyId, start, end)).ToList();
    }

    public async Task BlockDatesAsync(string propertyId, List<DateOnly> dates, string reason)
    {
        await _unitOfWork.BeginTransactionAsync();
        try
        {
            foreach (var date in dates)
            {
                var existing = await _unitOfWork.PropertyCalendars.GetByPropertyAndDateAsync(propertyId, date);

                if (existing != null)
                {
                    existing.IsAvailable = false;
                    existing.ReasonBlocked = reason;
                    existing.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    await _unitOfWork.PropertyCalendars.AddAsync(new PropertyCalendar
                    {
                        PropertyId = propertyId,
                        Date = date,
                        IsAvailable = false,
                        ReasonBlocked = reason
                    });
                }
            }

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    public async Task UnblockDatesAsync(string propertyId, List<DateOnly> dates)
    {
        await _unitOfWork.PropertyCalendars.UpdateLockStatusAsync(propertyId, dates, CalendarLockStatus.NONE);

        var calendars = await _unitOfWork.PropertyCalendars.GetByPropertyAndDateRangeAsync(
            propertyId,
            dates.Min(),
            dates.Max());

        foreach (var calendar in calendars.Where(c => dates.Contains(c.Date)))
        {
            calendar.IsAvailable = true;
            calendar.ReasonBlocked = null;
            calendar.UpdatedAt = DateTime.UtcNow;
        }

        await _unitOfWork.SaveChangesAsync();
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
