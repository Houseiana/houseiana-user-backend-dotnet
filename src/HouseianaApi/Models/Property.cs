using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HouseianaApi.Enums;

namespace HouseianaApi.Models;

[Table("properties")]
public class Property
{
    [Key]
    [Column("id")]
    public string Id { get; set; } = string.Empty;

    [Column("ownerId")]
    public string OwnerId { get; set; } = string.Empty;

    [Column("ownerType")]
    public OwnerType OwnerType { get; set; }

    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Column("description")]
    public string Description { get; set; } = string.Empty;

    [Column("propertyType")]
    public PropertyType PropertyType { get; set; }

    [Column("roomType")]
    public RoomType RoomType { get; set; }

    [Column("country")]
    public string Country { get; set; } = string.Empty;

    [Column("city")]
    public string City { get; set; } = string.Empty;

    [Column("address")]
    public string Address { get; set; } = string.Empty;

    [Column("latitude")]
    public double? Latitude { get; set; }

    [Column("longitude")]
    public double? Longitude { get; set; }

    [Column("guests")]
    public int Guests { get; set; }

    [Column("bedrooms")]
    public int Bedrooms { get; set; }

    [Column("beds")]
    public int Beds { get; set; }

    [Column("bathrooms")]
    public double Bathrooms { get; set; }

    [Column("pricePerNight")]
    public double PricePerNight { get; set; }

    [Column("cleaningFee")]
    public double CleaningFee { get; set; } = 0;

    [Column("serviceFee")]
    public double ServiceFee { get; set; } = 0;

    [Column("amenities", TypeName = "jsonb")]
    public string Amenities { get; set; } = "[]";

    [Column("photos", TypeName = "jsonb")]
    public string Photos { get; set; } = "[]";

    [Column("coverPhoto")]
    public string? CoverPhoto { get; set; }

    [Column("checkInTime")]
    public string CheckInTime { get; set; } = "15:00";

    [Column("checkOutTime")]
    public string CheckOutTime { get; set; } = "11:00";

    [Column("minNights")]
    public int MinNights { get; set; } = 1;

    [Column("maxNights")]
    public int? MaxNights { get; set; }

    [Column("instantBook")]
    public bool InstantBook { get; set; } = false;

    [Column("allowPets")]
    public bool AllowPets { get; set; } = false;

    [Column("allowSmoking")]
    public bool AllowSmoking { get; set; } = false;

    [Column("status")]
    public PropertyStatus Status { get; set; } = PropertyStatus.DRAFT;

    [Column("isActive")]
    public bool IsActive { get; set; } = true;

    [Column("averageRating")]
    public double? AverageRating { get; set; } = 0;

    [Column("cancellationPolicy")]
    public string CancellationPolicy { get; set; } = "FLEXIBLE";

    [Column("requestToBook")]
    public bool RequestToBook { get; set; } = false;

    [Column("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("OwnerId")]
    public virtual User? Owner { get; set; }

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    public virtual ICollection<PropertyCalendar> PropertyCalendars { get; set; } = new List<PropertyCalendar>();
}
