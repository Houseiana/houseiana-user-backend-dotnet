using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HouseianaApi.Models
{
    [Table("transactions")]
    public class Transaction
    {
        [Key]
        [Column("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Column("userId")]
        public string UserId { get; set; } = string.Empty;

        [Column("bookingId")]
        public string? BookingId { get; set; }

        [Column("description")]
        public string Description { get; set; } = string.Empty;

        [Column("amount")]
        public double Amount { get; set; }

        [Column("paymentMethod")]
        public string PaymentMethod { get; set; } = string.Empty;

        [Column("stripeChargeId")]
        public string? StripeChargeId { get; set; }

        [Column("stripeRefundId")]
        public string? StripeRefundId { get; set; }

        [Column("date")]
        public DateTime Date { get; set; } = DateTime.UtcNow;

        [Column("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updatedAt")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [Column("status")]
        public string Status { get; set; } = "PAID";

        [Column("type")]
        public string Type { get; set; } = string.Empty;

        // Navigation properties
        [ForeignKey("UserId")]
        public User? User { get; set; }

        [ForeignKey("BookingId")]
        public Booking? Booking { get; set; }
    }
}
