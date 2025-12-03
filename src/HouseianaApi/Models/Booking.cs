using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HouseianaApi.Enums;

namespace HouseianaApi.Models;

[Table("bookings")]
public class Booking
{
    [Key]
    [Column("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Column("propertyId")]
    public string PropertyId { get; set; } = string.Empty;

    [Column("guestId")]
    public string GuestId { get; set; } = string.Empty;

    [Column("hostId")]
    public string HostId { get; set; } = string.Empty;

    [Column("checkIn")]
    public DateTime CheckIn { get; set; }

    [Column("checkOut")]
    public DateTime CheckOut { get; set; }

    [Column("numberOfNights")]
    public int NumberOfNights { get; set; }

    [Column("guests")]
    public int Guests { get; set; }

    [Column("adults")]
    public int Adults { get; set; }

    [Column("children")]
    public int Children { get; set; } = 0;

    [Column("infants")]
    public int Infants { get; set; } = 0;

    [Column("nightlyRate")]
    public double NightlyRate { get; set; }

    [Column("subtotal")]
    public double Subtotal { get; set; }

    [Column("cleaningFee")]
    public double CleaningFee { get; set; } = 0;

    [Column("serviceFee")]
    public double ServiceFee { get; set; } = 0;

    [Column("taxAmount")]
    public double TaxAmount { get; set; } = 0;

    [Column("totalPrice")]
    public double TotalPrice { get; set; }

    [Column("platformCommission")]
    public double PlatformCommission { get; set; }

    [Column("hostEarnings")]
    public double HostEarnings { get; set; }

    [Column("status")]
    public BookingStatus Status { get; set; } = BookingStatus.PENDING;

    [Column("paymentStatus")]
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.PENDING;

    [Column("paymentMethod")]
    public string? PaymentMethod { get; set; }

    [Column("transactionId")]
    public string? TransactionId { get; set; }

    [Column("specialRequests")]
    public string? SpecialRequests { get; set; }

    [Column("arrivalTime")]
    public string? ArrivalTime { get; set; }

    [Column("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [Column("confirmedAt")]
    public DateTime? ConfirmedAt { get; set; }

    [Column("cancelledAt")]
    public DateTime? CancelledAt { get; set; }

    [Column("completedAt")]
    public DateTime? CompletedAt { get; set; }

    [Column("cancelledBy")]
    public string? CancelledBy { get; set; }

    [Column("cancellationReason")]
    public string? CancellationReason { get; set; }

    [Column("approvedAt")]
    public DateTime? ApprovedAt { get; set; }

    [Column("holdExpiresAt")]
    public DateTime? HoldExpiresAt { get; set; }

    [Column("cancellationPolicyType")]
    public string CancellationPolicyType { get; set; } = "FLEXIBLE";

    // Navigation properties
    [ForeignKey("PropertyId")]
    public virtual Property? Property { get; set; }

    [ForeignKey("GuestId")]
    public virtual User? Guest { get; set; }

    [ForeignKey("HostId")]
    public virtual User? Host { get; set; }
}
