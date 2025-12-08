using Houseiana.DAL.Models;

namespace Houseiana.Repositories;

public interface IAdminRepository : IRepository<Admin>
{
    Task<Admin?> GetByEmailAsync(string email);
    Task<Admin?> GetByUsernameAsync(string username);
    Task<Admin?> GetByEmailOrUsernameAsync(string emailOrUsername);
    Task<IEnumerable<Admin>> GetActiveAdminsAsync();
}
