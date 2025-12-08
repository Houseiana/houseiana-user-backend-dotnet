using Microsoft.EntityFrameworkCore;
using Houseiana.DAL.Models;
using Houseiana.DTOs;
using Houseiana.Enums;
using Houseiana.Repositories;
using Microsoft.Extensions.Logging;

namespace Houseiana.Business
{
    public class BookingsAdminService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<BookingsAdminService> _logger;

        public BookingsAdminService(IUnitOfWork unitOfWork, ILogger<BookingsAdminService> logger)
        {
            _unitOfWork = unitOfWork;
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

            var query = _unitOfWork.Bookings
                .Query()
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
            var booking = await _unitOfWork.Bookings.GetWithDetailsAsync(bookingId);

            if (booking == null)
            {
                return new ApiResponse<Booking> { Success = false, Message = "Booking not found" };
            }

            return new ApiResponse<Booking> { Success = true, Data = booking };
        }

        public async Task<ApiResponse<Booking>> UpdateBookingStatusAsync(string bookingId, string status, string? reason = null)
        {
            var booking = await _unitOfWork.Bookings.GetByIdAsync(bookingId);
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

            await _unitOfWork.SaveChangesAsync();

            return new ApiResponse<Booking>
            {
                Success = true,
                Message = $"Booking status updated to {status}",
                Data = booking
            };
        }

        public async Task<SupervisorStatsDto> GetStatsAsync()
        {
            var totalBookings = await _unitOfWork.Bookings.CountAsync();
            var pendingBookings = await _unitOfWork.Bookings.CountAsync(b => b.Status == BookingStatus.PENDING);
            var confirmedBookings = await _unitOfWork.Bookings.CountAsync(b => b.Status == BookingStatus.CONFIRMED);
            var completedBookings = await _unitOfWork.Bookings.CountAsync(b => b.Status == BookingStatus.COMPLETED);
            var cancelledBookings = await _unitOfWork.Bookings.CountAsync(b => b.Status == BookingStatus.CANCELLED);

            var paidBookings = await _unitOfWork.Bookings.FindAsync(b => b.PaymentStatus == PaymentStatus.PAID);
            var totalRevenue = paidBookings.Sum(b => b.TotalPrice);

            var totalProperties = await _unitOfWork.Properties.CountAsync();
            var activeProperties = await _unitOfWork.Properties.CountAsync(p => p.Status == PropertyStatus.ACTIVE && p.IsActive);

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
