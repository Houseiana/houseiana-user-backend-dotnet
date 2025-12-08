using Houseiana.Enums;
using Houseiana.DAL.Models;

namespace Houseiana.Repositories;

public interface IPropertyRepository : IRepository<Property>
{
    Task<IEnumerable<Property>> GetByOwnerIdAsync(string ownerId);
    Task<IEnumerable<Property>> GetByCityAsync(string city);
    Task<IEnumerable<Property>> GetByStatusAsync(PropertyStatus status);
    Task<IEnumerable<Property>> GetActivePropertiesAsync();
    Task<Property?> GetWithOwnerAsync(string propertyId);
    Task<Property?> GetWithBookingsAsync(string propertyId);
    Task<Property?> GetWithCalendarAsync(string propertyId);
    Task<Property?> GetFullPropertyAsync(string propertyId);
}
