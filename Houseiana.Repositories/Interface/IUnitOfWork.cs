using Microsoft.EntityFrameworkCore.Storage;

namespace Houseiana.Repositories;

public interface IUnitOfWork : IDisposable
{
    // Repositories
    IUserRepository Users { get; }
    IPropertyRepository Properties { get; }
    IBookingRepository Bookings { get; }
    IPropertyCalendarRepository PropertyCalendars { get; }
    ITransactionRepository Transactions { get; }
    IAdminRepository Admins { get; }
    IPropertyApprovalRepository PropertyApprovals { get; }

    // Transaction management
    Task<int> SaveChangesAsync();
    Task<IDbContextTransaction> BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}
