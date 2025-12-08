using Microsoft.EntityFrameworkCore;
using Houseiana.DAL;
using Houseiana.Enums;
using Houseiana.DAL.Models;

namespace Houseiana.Repositories;

public class BookingRepository : Repository<Booking>, IBookingRepository
{
    public BookingRepository(HouseianaDbContext context) : base(context)
    {
    }

    public async Task<Booking?> GetWithDetailsAsync(string bookingId)
    {
        return await _dbSet
            .Include(b => b.Property)
            .Include(b => b.Guest)
            .Include(b => b.Host)
            .FirstOrDefaultAsync(b => b.Id == bookingId);
    }

    public async Task<IEnumerable<Booking>> GetByGuestIdAsync(string guestId)
    {
        return await _dbSet
            .Where(b => b.GuestId == guestId)
            .Include(b => b.Property)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Booking>> GetByHostIdAsync(string hostId)
    {
        return await _dbSet
            .Where(b => b.HostId == hostId)
            .Include(b => b.Property)
            .Include(b => b.Guest)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Booking>> GetByPropertyIdAsync(string propertyId)
    {
        return await _dbSet
            .Where(b => b.PropertyId == propertyId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Booking>> GetByStatusAsync(BookingStatus status)
    {
        return await _dbSet
            .Where(b => b.Status == status)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Booking>> GetByPaymentStatusAsync(PaymentStatus paymentStatus)
    {
        return await _dbSet
            .Where(b => b.PaymentStatus == paymentStatus)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Booking>> GetExpiredHoldsAsync(DateTime cutoffTime)
    {
        return await _dbSet
            .Where(b => b.Status == BookingStatus.PENDING &&
                        b.HoldExpiresAt != null &&
                        b.HoldExpiresAt < cutoffTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<Booking>> GetUserBookingsAsync(string userId, string role)
    {
        var query = role.ToLower() switch
        {
            "guest" => _dbSet.Where(b => b.GuestId == userId),
            "host" => _dbSet.Where(b => b.HostId == userId),
            _ => _dbSet.Where(b => b.GuestId == userId || b.HostId == userId)
        };

        return await query
            .Include(b => b.Property)
            .Include(b => b.Guest)
            .Include(b => b.Host)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
    }
}
