using System.ComponentModel.DataAnnotations;

namespace Houseiana.DTOs;

public class CreateBookingDto
{
    [Required]
    public string PropertyId { get; set; } = string.Empty;

    [Required]
    public string GuestId { get; set; } = string.Empty;

    [Required]
    public string HostId { get; set; } = string.Empty;

    [Required]
    public DateTime CheckIn { get; set; }

    [Required]
    public DateTime CheckOut { get; set; }

    [Required]
    public int Guests { get; set; }

    [Required]
    public int Adults { get; set; }

    public int Children { get; set; } = 0;

    public int Infants { get; set; } = 0;

    [Required]
    public double NightlyRate { get; set; }

    [Required]
    public double Subtotal { get; set; }

    public double CleaningFee { get; set; } = 0;

    public double ServiceFee { get; set; } = 0;

    public double TaxAmount { get; set; } = 0;

    [Required]
    public double TotalPrice { get; set; }

    [Required]
    public double PlatformCommission { get; set; }

    [Required]
    public double HostEarnings { get; set; }

    public string? SpecialRequests { get; set; }

    public string? ArrivalTime { get; set; }

    public bool InstantBook { get; set; } = false;
}

public class ApproveRejectBookingDto
{
    [Required]
    public string HostId { get; set; } = string.Empty;

    public string? Reason { get; set; }
}

public class CancelBookingDto
{
    [Required]
    public string UserId { get; set; } = string.Empty;

    public string? Reason { get; set; }
}

public class BookingResponseDto<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
    public int? Count { get; set; }
}
