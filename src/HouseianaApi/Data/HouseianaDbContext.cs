using Microsoft.EntityFrameworkCore;
using HouseianaApi.Models;
using HouseianaApi.Enums;

namespace HouseianaApi.Data;

public class HouseianaDbContext : DbContext
{
    public HouseianaDbContext(DbContextOptions<HouseianaDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Property> Properties { get; set; }
    public DbSet<Booking> Bookings { get; set; }
    public DbSet<PropertyCalendar> PropertyCalendars { get; set; }
    public DbSet<Admin> Admins { get; set; }
    public DbSet<PropertyApproval> PropertyApprovals { get; set; }
    public DbSet<Transaction> Transactions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure User
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.ClerkId).IsUnique();

            entity.Property(e => e.AccountStatus)
                .HasConversion<string>();
            entity.Property(e => e.KycStatus)
                .HasConversion<string>();
        });

        // Configure Property
        modelBuilder.Entity<Property>(entity =>
        {
            entity.HasIndex(e => e.OwnerId);
            entity.HasIndex(e => e.City);
            entity.HasIndex(e => e.Status);

            entity.Property(e => e.OwnerType)
                .HasConversion<string>();
            entity.Property(e => e.PropertyType)
                .HasConversion<string>();
            entity.Property(e => e.RoomType)
                .HasConversion<string>();
            entity.Property(e => e.Status)
                .HasConversion<string>();

            entity.HasOne(e => e.Owner)
                .WithMany(u => u.Properties)
                .HasForeignKey(e => e.OwnerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Booking
        modelBuilder.Entity<Booking>(entity =>
        {
            entity.HasIndex(e => e.PropertyId);
            entity.HasIndex(e => e.GuestId);
            entity.HasIndex(e => e.HostId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.PaymentStatus);
            entity.HasIndex(e => e.HoldExpiresAt);

            entity.Property(e => e.Status)
                .HasConversion<string>();
            entity.Property(e => e.PaymentStatus)
                .HasConversion<string>();

            entity.HasOne(e => e.Property)
                .WithMany(p => p.Bookings)
                .HasForeignKey(e => e.PropertyId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Guest)
                .WithMany(u => u.GuestBookings)
                .HasForeignKey(e => e.GuestId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Host)
                .WithMany(u => u.HostBookings)
                .HasForeignKey(e => e.HostId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure PropertyCalendar
        modelBuilder.Entity<PropertyCalendar>(entity =>
        {
            entity.HasIndex(e => new { e.PropertyId, e.Date }).IsUnique();
            entity.HasIndex(e => new { e.PropertyId, e.Date, e.LockStatus });
            entity.HasIndex(e => e.LockExpiresAt);

            entity.Property(e => e.LockStatus)
                .HasConversion<string>();

            entity.HasOne(e => e.Property)
                .WithMany(p => p.PropertyCalendars)
                .HasForeignKey(e => e.PropertyId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
