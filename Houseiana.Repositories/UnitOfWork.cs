using Microsoft.EntityFrameworkCore.Storage;
using Houseiana.DAL;

namespace Houseiana.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly HouseianaDbContext _context;
    private IDbContextTransaction? _currentTransaction;

    private IUserRepository? _users;
    private IPropertyRepository? _properties;
    private IBookingRepository? _bookings;
    private IPropertyCalendarRepository? _propertyCalendars;
    private ITransactionRepository? _transactions;
    private IAdminRepository? _admins;
    private IPropertyApprovalRepository? _propertyApprovals;

    public UnitOfWork(HouseianaDbContext context)
    {
        _context = context;
    }

    public IUserRepository Users =>
        _users ??= new UserRepository(_context);

    public IPropertyRepository Properties =>
        _properties ??= new PropertyRepository(_context);

    public IBookingRepository Bookings =>
        _bookings ??= new BookingRepository(_context);

    public IPropertyCalendarRepository PropertyCalendars =>
        _propertyCalendars ??= new PropertyCalendarRepository(_context);

    public ITransactionRepository Transactions =>
        _transactions ??= new TransactionRepository(_context);

    public IAdminRepository Admins =>
        _admins ??= new AdminRepository(_context);

    public IPropertyApprovalRepository PropertyApprovals =>
        _propertyApprovals ??= new PropertyApprovalRepository(_context);

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public async Task<IDbContextTransaction> BeginTransactionAsync()
    {
        _currentTransaction = await _context.Database.BeginTransactionAsync();
        return _currentTransaction;
    }

    public async Task CommitTransactionAsync()
    {
        if (_currentTransaction != null)
        {
            await _currentTransaction.CommitAsync();
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }
    }

    public async Task RollbackTransactionAsync()
    {
        if (_currentTransaction != null)
        {
            await _currentTransaction.RollbackAsync();
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }
    }

    public void Dispose()
    {
        _currentTransaction?.Dispose();
        _context.Dispose();
    }
}
