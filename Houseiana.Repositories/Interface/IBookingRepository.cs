using Houseiana.Enums;
using Houseiana.DAL.Models;

namespace Houseiana.Repositories;

public interface IBookingRepository : IRepository<Booking>
{
    Task<Booking?> GetWithDetailsAsync(string bookingId);
    Task<IEnumerable<Booking>> GetByGuestIdAsync(string guestId);
    Task<IEnumerable<Booking>> GetByHostIdAsync(string hostId);
    Task<IEnumerable<Booking>> GetByPropertyIdAsync(string propertyId);
    Task<IEnumerable<Booking>> GetByStatusAsync(BookingStatus status);
    Task<IEnumerable<Booking>> GetByPaymentStatusAsync(PaymentStatus paymentStatus);
    Task<IEnumerable<Booking>> GetExpiredHoldsAsync(DateTime cutoffTime);
    Task<IEnumerable<Booking>> GetUserBookingsAsync(string userId, string role);
}
