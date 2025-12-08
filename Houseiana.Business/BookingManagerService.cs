using Houseiana.DAL.Models;
using Houseiana.DTOs;
using Houseiana.Enums;
using Houseiana.Repositories;
using Microsoft.Extensions.Logging;

namespace Houseiana.Business;

public interface IBookingManagerService
{
    Task<BookingResponseDto<Booking>> CreateBookingAsync(CreateBookingDto dto);
    Task<BookingResponseDto<Booking>> ConfirmBookingAsync(string bookingId);
    Task<BookingResponseDto<Booking>> ApproveBookingAsync(string bookingId, string hostId);
    Task<BookingResponseDto<Booking>> RejectBookingAsync(string bookingId, string hostId, string? reason);
    Task<BookingResponseDto<Booking>> CancelBookingAsync(string bookingId, string userId, string? reason);
    Task<BookingResponseDto<Booking>> GetBookingAsync(string bookingId);
    Task<BookingResponseDto<List<Booking>>> GetUserBookingsAsync(string userId, string role);
}

public class BookingManagerService : IBookingManagerService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAvailabilityService _availabilityService;


    private readonly ILogger<BookingManagerService> _logger;

    public BookingManagerService(
        IUnitOfWork unitOfWork,
        IAvailabilityService availabilityService,
        ILogger<BookingManagerService> logger)
    {
        _unitOfWork = unitOfWork;
        _availabilityService = availabilityService;
        _logger = logger;
    }

    public async Task<BookingResponseDto<Booking>> CreateBookingAsync(CreateBookingDto dto)
    {
        var checkIn = dto.CheckIn;
        var checkOut = dto.CheckOut;
        var now = DateTime.UtcNow;

        if (checkIn < now)
        {
            throw new ArgumentException("Check-in date must be in the future");
        }

        if (checkOut <= checkIn)
        {
            throw new ArgumentException("Check-out date must be after check-in date");
        }

        var numberOfNights = (int)Math.Ceiling((checkOut - checkIn).TotalDays);

        var isAvailable = await _availabilityService.CheckAvailabilityAsync(dto.PropertyId, checkIn, checkOut);
        if (!isAvailable)
        {
            throw new InvalidOperationException("Selected dates are not available");
        }

        var property = await _unitOfWork.Properties.GetByIdAsync(dto.PropertyId);
        if (property == null)
        {
            throw new KeyNotFoundException("Property not found");
        }

        var guest = await _unitOfWork.Users.GetByIdAsync(dto.GuestId);
        if (guest == null)
        {
            throw new KeyNotFoundException("Guest not found");
        }

        var initialStatus = dto.InstantBook || property.InstantBook
            ? BookingStatus.PENDING
            : BookingStatus.REQUESTED;

        var booking = new Booking
        {
            PropertyId = dto.PropertyId,
            GuestId = dto.GuestId,
            HostId = dto.HostId,
            CheckIn = checkIn,
            CheckOut = checkOut,
            NumberOfNights = numberOfNights,
            Guests = dto.Guests,
            Adults = dto.Adults,
            Children = dto.Children,
            Infants = dto.Infants,
            NightlyRate = dto.NightlyRate,
            Subtotal = dto.Subtotal,
            CleaningFee = dto.CleaningFee,
            ServiceFee = dto.ServiceFee,
            TaxAmount = dto.TaxAmount,
            TotalPrice = dto.TotalPrice,
            PlatformCommission = dto.PlatformCommission,
            HostEarnings = dto.HostEarnings,
            Status = initialStatus,
            PaymentStatus = PaymentStatus.PENDING,
            SpecialRequests = dto.SpecialRequests,
            ArrivalTime = dto.ArrivalTime,
            HoldExpiresAt = DateTime.UtcNow.AddMinutes(dto.InstantBook ? 15 : 1440)
        };

        await _unitOfWork.Bookings.AddAsync(booking);
        await _unitOfWork.SaveChangesAsync();

        try
        {
            var holdDuration = dto.InstantBook ? 15 : 1440;
            await _availabilityService.CreateSoftHoldAsync(
                dto.PropertyId,
                booking.Id,
                checkIn,
                checkOut,
                holdDuration);

            _logger.LogInformation("Created soft-hold for booking {BookingId} ({HoldDuration} minutes)", booking.Id, holdDuration);
        }
        catch (Exception ex)
        {
            _unitOfWork.Bookings.Remove(booking);
            await _unitOfWork.SaveChangesAsync();
            throw new InvalidOperationException($"Failed to secure booking dates: {ex.Message}");
        }

        var createdBooking = await _unitOfWork.Bookings.GetWithDetailsAsync(booking.Id);

        return new BookingResponseDto<Booking>
        {
            Success = true,
            Data = createdBooking,
            Message = initialStatus == BookingStatus.REQUESTED
                ? "Booking request submitted. Awaiting host approval."
                : "Booking created. Please complete payment within 15 minutes."
        };
    }

    public async Task<BookingResponseDto<Booking>> ConfirmBookingAsync(string bookingId)
    {
        var booking = await _unitOfWork.Bookings.GetWithDetailsAsync(bookingId);

        if (booking == null)
        {
            throw new KeyNotFoundException("Booking not found");
        }

        if (booking.Status == BookingStatus.CONFIRMED)
        {
            return new BookingResponseDto<Booking>
            {
                Success = true,
                Data = booking,
                Message = "Booking already confirmed"
            };
        }

        if (booking.Status == BookingStatus.CANCELLED || booking.Status == BookingStatus.REJECTED)
        {
            throw new InvalidOperationException("Cannot confirm a cancelled or rejected booking");
        }

        try
        {
            await _availabilityService.ConfirmHoldAsync(
                booking.PropertyId,
                booking.Id,
                booking.CheckIn,
                booking.CheckOut);

            _logger.LogInformation("Confirmed hold for booking {BookingId}", booking.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to confirm hold: {Message}", ex.Message);
            throw new InvalidOperationException($"Failed to confirm booking dates: {ex.Message}");
        }

        booking.Status = BookingStatus.CONFIRMED;
        booking.ConfirmedAt = DateTime.UtcNow;
        booking.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync();

        return new BookingResponseDto<Booking>
        {
            Success = true,
            Data = booking,
            Message = "Booking confirmed successfully"
        };
    }

    public async Task<BookingResponseDto<Booking>> ApproveBookingAsync(string bookingId, string hostId)
    {
        var booking = await _unitOfWork.Bookings.GetWithDetailsAsync(bookingId);

        if (booking == null)
        {
            throw new KeyNotFoundException("Booking not found");
        }

        if (booking.HostId != hostId)
        {
            throw new InvalidOperationException("Only the host can approve this booking");
        }

        if (booking.Status != BookingStatus.REQUESTED)
        {
            throw new InvalidOperationException("Booking is not awaiting approval");
        }

        booking.Status = BookingStatus.APPROVED;
        booking.ApprovedAt = DateTime.UtcNow;
        booking.UpdatedAt = DateTime.UtcNow;
        booking.HoldExpiresAt = DateTime.UtcNow.AddHours(24);

        await _unitOfWork.SaveChangesAsync();

        try
        {
            await _availabilityService.ExtendHoldAsync(
                booking.PropertyId,
                booking.Id,
                booking.CheckIn,
                booking.CheckOut,
                1440);

            _logger.LogInformation("Extended hold for booking {BookingId} (24 hours)", booking.Id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to extend hold: {Message}", ex.Message);
        }

        return new BookingResponseDto<Booking>
        {
            Success = true,
            Data = booking,
            Message = "Booking approved. Guest has 24 hours to complete payment."
        };
    }

    public async Task<BookingResponseDto<Booking>> RejectBookingAsync(string bookingId, string hostId, string? reason)
    {
        var booking = await _unitOfWork.Bookings.GetWithDetailsAsync(bookingId);

        if (booking == null)
        {
            throw new KeyNotFoundException("Booking not found");
        }

        if (booking.HostId != hostId)
        {
            throw new InvalidOperationException("Only the host can reject this booking");
        }

        if (booking.Status != BookingStatus.REQUESTED)
        {
            throw new InvalidOperationException("Booking is not awaiting approval");
        }

        try
        {
            await _availabilityService.ReleaseLockAsync(
                booking.PropertyId,
                booking.Id,
                booking.CheckIn,
                booking.CheckOut);

            _logger.LogInformation("Released lock for rejected booking {BookingId}", booking.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to release lock: {Message}", ex.Message);
        }

        booking.Status = BookingStatus.REJECTED;
        booking.CancellationReason = reason ?? "Rejected by host";
        booking.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync();

        return new BookingResponseDto<Booking>
        {
            Success = true,
            Data = booking,
            Message = "Booking rejected"
        };
    }

    public async Task<BookingResponseDto<Booking>> CancelBookingAsync(string bookingId, string userId, string? reason)
    {
        var booking = await _unitOfWork.Bookings.GetWithDetailsAsync(bookingId);

        if (booking == null)
        {
            throw new KeyNotFoundException("Booking not found");
        }

        if (booking.GuestId != userId && booking.HostId != userId)
        {
            throw new InvalidOperationException("Only the guest or host can cancel this booking");
        }

        if (booking.Status == BookingStatus.CANCELLED)
        {
            return new BookingResponseDto<Booking>
            {
                Success = true,
                Data = booking,
                Message = "Booking already cancelled"
            };
        }

        if (booking.Status == BookingStatus.COMPLETED)
        {
            throw new InvalidOperationException("Cannot cancel a completed booking");
        }

        try
        {
            await _availabilityService.ReleaseLockAsync(
                booking.PropertyId,
                booking.Id,
                booking.CheckIn,
                booking.CheckOut);

            _logger.LogInformation("Released lock for cancelled booking {BookingId}", booking.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to release lock: {Message}", ex.Message);
        }

        booking.Status = BookingStatus.CANCELLED;
        booking.CancelledAt = DateTime.UtcNow;
        booking.CancelledBy = userId;
        booking.CancellationReason = reason;
        booking.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync();

        return new BookingResponseDto<Booking>
        {
            Success = true,
            Data = booking,
            Message = "Booking cancelled successfully"
        };
    }

    public async Task<BookingResponseDto<Booking>> GetBookingAsync(string bookingId)
    {
        var booking = await _unitOfWork.Bookings.GetWithDetailsAsync(bookingId);

        if (booking == null)
        {
            throw new KeyNotFoundException("Booking not found");
        }

        return new BookingResponseDto<Booking>
        {
            Success = true,
            Data = booking
        };
    }

    public async Task<BookingResponseDto<List<Booking>>> GetUserBookingsAsync(string userId, string role)
    {
        var bookings = (await _unitOfWork.Bookings.GetUserBookingsAsync(userId, role)).ToList();

        return new BookingResponseDto<List<Booking>>
        {
            Success = true,
            Data = bookings,
            Count = bookings.Count
        };
    }
}
