using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Houseiana.Enums;

namespace Houseiana.DAL.Models;

[Table("users")]
public class User
{
    [Key]
    [Column("id")]
    public string Id { get; set; } = string.Empty;

    [Column("firstName")]
    public string FirstName { get; set; } = string.Empty;

    [Column("lastName")]
    public string LastName { get; set; } = string.Empty;

    [Column("email")]
    public string Email { get; set; } = string.Empty;

    [Column("phone")]
    public string? Phone { get; set; }

    [Column("countryCode")]
    public string CountryCode { get; set; } = "+974";

    [Column("clerkId")]
    public string? ClerkId { get; set; }

    [Column("emailVerified")]
    public bool EmailVerified { get; set; } = false;

    [Column("phoneVerified")]
    public bool PhoneVerified { get; set; } = false;

    [Column("profilePhoto")]
    public string? ProfilePhoto { get; set; }

    [Column("nationality")]
    public string Nationality { get; set; } = "Qatar";

    [Column("preferredLanguage")]
    public string PreferredLanguage { get; set; } = "en";

    [Column("preferredCurrency")]
    public string PreferredCurrency { get; set; } = "QAR";

    [Column("isGuest")]
    public bool IsGuest { get; set; } = true;

    [Column("isHost")]
    public bool IsHost { get; set; } = false;

    [Column("isAdmin")]
    public bool IsAdmin { get; set; } = false;

    [Column("accountStatus")]
    public UserStatus AccountStatus { get; set; } = UserStatus.ACTIVE;

    [Column("kycStatus")]
    public KycStatus KycStatus { get; set; } = KycStatus.PENDING;

    [Column("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [Column("lastLoginAt")]
    public DateTime? LastLoginAt { get; set; }

    // Navigation properties
    public virtual ICollection<Booking> GuestBookings { get; set; } = new List<Booking>();
    public virtual ICollection<Booking> HostBookings { get; set; } = new List<Booking>();
    public virtual ICollection<Property> Properties { get; set; } = new List<Property>();
}
