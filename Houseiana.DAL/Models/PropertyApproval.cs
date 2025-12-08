using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Houseiana.DAL.Models
{
    [Table("property_approvals")]
    public class PropertyApproval
    {
        [Key]
        [Column("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Column("propertyId")]
        public string PropertyId { get; set; } = string.Empty;

        [Column("adminId")]
        public string AdminId { get; set; } = string.Empty;

        [Column("comments")]
        public string? Comments { get; set; }

        [Column("changes", TypeName = "jsonb")]
        public string? Changes { get; set; }

        [Column("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("status")]
        public string Status { get; set; } = "PENDING";

        // Navigation properties
        [ForeignKey("PropertyId")]
        public Property? Property { get; set; }

        [ForeignKey("AdminId")]
        public Admin? Admin { get; set; }
    }
}
