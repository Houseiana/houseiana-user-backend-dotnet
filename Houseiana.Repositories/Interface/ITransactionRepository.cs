using Houseiana.DAL.Models;

namespace Houseiana.Repositories;

public interface ITransactionRepository : IRepository<Transaction>
{
    Task<IEnumerable<Transaction>> GetByUserIdAsync(string userId);
    Task<IEnumerable<Transaction>> GetByBookingIdAsync(string bookingId);
    Task<IEnumerable<Transaction>> GetByStatusAsync(string status);
    Task<IEnumerable<Transaction>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
}
