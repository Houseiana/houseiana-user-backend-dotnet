using Microsoft.EntityFrameworkCore;
using HouseianaApi.Data;
using HouseianaApi.Models;
using HouseianaApi.DTOs;
using HouseianaApi.Enums;

namespace HouseianaApi.Services
{
    public class BookingsAdminService
    {
        private readonly HouseianaDbContext _context;
        private readonly ILogger<BookingsAdminService> _logger;

        public BookingsAdminService(HouseianaDbContext context, ILogger<BookingsAdminService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ApiResponse<List<Booking>>> GetBookingsAsync(
            int page = 1,
            int limit = 20,
            string? status = null,
            string? guestId = null,
            string? hostId = null,
            string? propertyId = null)
        {
            var skip = (page - 1) * limit;

            var query = _context.Bookings
                .Include(b => b.Property)
                .Include(b => b.Guest)
                .Include(b => b.Host)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status) && Enum.TryParse<BookingStatus>(status, true, out var bookingStatus))
            {
                query = query.Where(b => b.Status == bookingStatus);
            }

            if (!string.IsNullOrEmpty(guestId))
            {
                query = query.Where(b => b.GuestId == guestId);
            }

            if (!string.IsNullOrEmpty(hostId))
            {
                query = query.Where(b => b.HostId == hostId);
            }

            if (!string.IsNullOrEmpty(propertyId))
            {
                query = query.Where(b => b.PropertyId == propertyId);
            }

            var total = await query.CountAsync();
            var bookings = await query
                .OrderByDescending(b => b.CreatedAt)
                .Skip(skip)
                .Take(limit)
                .ToListAsync();

            return new ApiResponse<List<Booking>>
            {
                Success = true,
                Data = bookings,
                Pagination = new PaginationDto
                {
                    Page = page,
                    Limit = limit,
                    Total = total,
                    TotalPages = (int)Math.Ceiling(total / (double)limit)
                }
            };
        }

        public async Task<ApiResponse<Booking>> GetBookingByIdAsync(string bookingId)
        {
            var booking = await _context.Bookings
                .Include(b => b.Property)
                .Include(b => b.Guest)
                .Include(b => b.Host)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null)
            {
                return new ApiResponse<Booking> { Success = false, Message = "Booking not found" };
            }

            return new ApiResponse<Booking> { Success = true, Data = booking };
        }

        public async Task<ApiResponse<Booking>> UpdateBookingStatusAsync(string bookingId, string status, string? reason = null)
        {
            var booking = await _context.Bookings.FindAsync(bookingId);
            if (booking == null)
            {
                return new ApiResponse<Booking> { Success = false, Message = "Booking not found" };
            }

            if (!Enum.TryParse<BookingStatus>(status, true, out var newStatus))
            {
                return new ApiResponse<Booking> { Success = false, Message = "Invalid booking status" };
            }

            booking.Status = newStatus;
            booking.UpdatedAt = DateTime.UtcNow;

            if (newStatus == BookingStatus.CANCELLED && !string.IsNullOrEmpty(reason))
            {
                booking.CancellationReason = reason;
                booking.CancelledAt = DateTime.UtcNow;
            }

            if (newStatus == BookingStatus.CONFIRMED)
            {
                booking.ConfirmedAt = DateTime.UtcNow;
            }

            if (newStatus == BookingStatus.COMPLETED)
            {
                booking.CompletedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            return new ApiResponse<Booking>
            {
                Success = true,
                Message = $"Booking status updated to {status}",
                Data = booking
            };
        }

        public async Task<SupervisorStatsDto> GetStatsAsync()
        {
            var totalBookings = await _context.Bookings.CountAsync();
            var pendingBookings = await _context.Bookings.CountAsync(b => b.Status == BookingStatus.PENDING);
            var confirmedBookings = await _context.Bookings.CountAsync(b => b.Status == BookingStatus.CONFIRMED);
            var completedBookings = await _context.Bookings.CountAsync(b => b.Status == BookingStatus.COMPLETED);
            var cancelledBookings = await _context.Bookings.CountAsync(b => b.Status == BookingStatus.CANCELLED);
            var totalRevenue = await _context.Bookings.Where(b => b.PaymentStatus == PaymentStatus.PAID).SumAsync(b => b.TotalPrice);
            var totalProperties = await _context.Properties.CountAsync();
            var activeProperties = await _context.Properties.CountAsync(p => p.Status == PropertyStatus.ACTIVE && p.IsActive);

            return new SupervisorStatsDto
            {
                TotalBookings = totalBookings,
                PendingBookings = pendingBookings,
                ConfirmedBookings = confirmedBookings,
                CompletedBookings = completedBookings,
                CancelledBookings = cancelledBookings,
                TotalRevenue = totalRevenue,
                TotalProperties = totalProperties,
                ActiveProperties = activeProperties
            };
        }
    }
}
