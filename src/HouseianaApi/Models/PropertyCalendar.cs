using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HouseianaApi.Enums;

namespace HouseianaApi.Models;

[Table("property_calendars")]
public class PropertyCalendar
{
    [Key]
    [Column("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Column("propertyId")]
    public string PropertyId { get; set; } = string.Empty;

    [Column("date")]
    public DateOnly Date { get; set; }

    [Column("pricePerNight")]
    public double? PricePerNight { get; set; }

    [Column("isAvailable")]
    public bool IsAvailable { get; set; } = true;

    [Column("reasonBlocked")]
    public string? ReasonBlocked { get; set; }

    [Column("lockStatus")]
    public CalendarLockStatus LockStatus { get; set; } = CalendarLockStatus.NONE;

    [Column("lockBookingId")]
    public string? LockBookingId { get; set; }

    [Column("lockExpiresAt")]
    public DateTime? LockExpiresAt { get; set; }

    [Column("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("PropertyId")]
    public virtual Property? Property { get; set; }
}
