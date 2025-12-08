using Houseiana.DAL.Models;

namespace Houseiana.Repositories;

public interface IPropertyApprovalRepository : IRepository<PropertyApproval>
{
    Task<IEnumerable<PropertyApproval>> GetByPropertyIdAsync(string propertyId);
    Task<IEnumerable<PropertyApproval>> GetByAdminIdAsync(string adminId);
    Task<PropertyApproval?> GetLatestByPropertyIdAsync(string propertyId);
}
