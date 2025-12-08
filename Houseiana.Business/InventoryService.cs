using Microsoft.EntityFrameworkCore;
using Houseiana.DAL.Models;
using Houseiana.DTOs;
using Houseiana.Enums;
using Houseiana.Repositories;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Houseiana.Business
{
    public class InventoryService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _configuration;
        private readonly ILogger<InventoryService> _logger;

        public InventoryService(IUnitOfWork unitOfWork, IConfiguration configuration, ILogger<InventoryService> logger)
        {
            _unitOfWork = unitOfWork;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<AdminLoginResponseDto> LoginAsync(string email, string password)
        {
            var admin = await _unitOfWork.Admins.GetByEmailOrUsernameAsync(email);

            if (admin == null)
            {
                return new AdminLoginResponseDto { Success = false };
            }

            var isValid = BCrypt.Net.BCrypt.Verify(password, admin.Password);
            if (!isValid)
            {
                return new AdminLoginResponseDto { Success = false };
            }

            admin.LastLogin = DateTime.UtcNow;
            await _unitOfWork.SaveChangesAsync();

            var token = GenerateJwtToken(admin);

            return new AdminLoginResponseDto
            {
                Success = true,
                Token = token,
                User = new AdminUserDto
                {
                    Id = admin.Id,
                    Email = admin.Email,
                    Username = admin.Username,
                    FullName = admin.FullName,
                    Role = admin.Role
                }
            };
        }

        private string GenerateJwtToken(Admin admin)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Secret"] ?? "your-super-secret-key-minimum-32-characters"));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, admin.Id),
                new Claim(ClaimTypes.Email, admin.Email),
                new Claim(ClaimTypes.Name, admin.Username),
                new Claim(ClaimTypes.Role, admin.Role)
            };

            var token = new JwtSecurityToken(
                issuer: "HouseianaApi",
                audience: "HouseianaApi",
                claims: claims,
                expires: DateTime.UtcNow.AddDays(7),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<ApiResponse<DashboardKPIsDto>> GetDashboardKPIsAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            startDate ??= DateTime.UtcNow.AddDays(-30);
            endDate ??= DateTime.UtcNow;

            var totalProperties = await _unitOfWork.Properties.CountAsync();
            var activeProperties = await _unitOfWork.Properties.CountAsync(p => p.Status == PropertyStatus.ACTIVE && p.IsActive);
            var suspendedProperties = await _unitOfWork.Properties.CountAsync(p => p.Status == PropertyStatus.SUSPENDED);
            var pendingApprovals = await _unitOfWork.Properties.CountAsync(p => p.Status == PropertyStatus.PENDING || p.Status == PropertyStatus.DRAFT);
            var totalHosts = await _unitOfWork.Users.CountAsync(u => u.IsHost);

            var properties = await _unitOfWork.Properties.GetAllAsync();
            var avgRating = properties.Any() ? properties.Average(p => p.AverageRating ?? 0) : 0;

            return new ApiResponse<DashboardKPIsDto>
            {
                Success = true,
                Data = new DashboardKPIsDto
                {
                    TotalProperties = totalProperties,
                    ActiveProperties = activeProperties,
                    SuspendedProperties = suspendedProperties,
                    PendingApprovals = pendingApprovals,
                    TotalHosts = totalHosts,
                    TotalRevenue = 0,
                    RevenueChange = 0,
                    OccupancyRate = 0,
                    OccupancyChange = 0,
                    AverageRating = avgRating,
                    RatingChange = 0,
                    HostsChange = 0,
                    Currency = "USD",
                    Period = new PeriodDto
                    {
                        StartDate = startDate.Value.ToString("o"),
                        EndDate = endDate.Value.ToString("o")
                    }
                }
            };
        }

        public async Task<ApiResponse<List<Property>>> GetPendingApprovalsAsync(int page = 1, int limit = 20, string sortBy = "createdAt", string sortOrder = "desc")
        {
            var skip = (page - 1) * limit;

            var allPending = await _unitOfWork.Properties
                .Query()
                .Include(p => p.Owner)
                .Where(p => p.Status == PropertyStatus.PENDING || p.Status == PropertyStatus.DRAFT)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            var total = allPending.Count;
            var properties = allPending.Skip(skip).Take(limit).ToList();

            return new ApiResponse<List<Property>>
            {
                Success = true,
                Data = properties,
                Pagination = new PaginationDto
                {
                    Page = page,
                    Limit = limit,
                    Total = total,
                    TotalPages = (int)Math.Ceiling(total / (double)limit)
                }
            };
        }

        public async Task<ApiResponse<Property>> ApprovePropertyAsync(string propertyId, string adminId, string? notes)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var property = await _unitOfWork.Properties.GetByIdAsync(propertyId);
                if (property == null)
                {
                    return new ApiResponse<Property> { Success = false, Message = "Property not found" };
                }

                property.Status = PropertyStatus.ACTIVE;
                property.IsActive = true;
                property.UpdatedAt = DateTime.UtcNow;

                var approval = new PropertyApproval
                {
                    PropertyId = propertyId,
                    AdminId = adminId,
                    Status = "APPROVED",
                    Comments = notes
                };
                await _unitOfWork.PropertyApprovals.AddAsync(approval);

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                return new ApiResponse<Property>
                {
                    Success = true,
                    Message = "Property approved successfully",
                    Data = property
                };
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Error approving property {PropertyId}", propertyId);
                throw;
            }
        }

        public async Task<ApiResponse<Property>> RejectPropertyAsync(string propertyId, string adminId, string reason, List<string>? changesRequested)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var property = await _unitOfWork.Properties.GetByIdAsync(propertyId);
                if (property == null)
                {
                    return new ApiResponse<Property> { Success = false, Message = "Property not found" };
                }

                property.Status = PropertyStatus.REJECTED;
                property.UpdatedAt = DateTime.UtcNow;

                var approval = new PropertyApproval
                {
                    PropertyId = propertyId,
                    AdminId = adminId,
                    Status = "REJECTED",
                    Comments = reason,
                    Changes = changesRequested != null ? System.Text.Json.JsonSerializer.Serialize(changesRequested) : null
                };
                await _unitOfWork.PropertyApprovals.AddAsync(approval);

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                return new ApiResponse<Property>
                {
                    Success = true,
                    Message = "Property rejected",
                    Data = property
                };
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Error rejecting property {PropertyId}", propertyId);
                throw;
            }
        }

        public async Task<ApiResponse<Property>> SuspendPropertyAsync(string propertyId, string adminId, string reason)
        {
            var property = await _unitOfWork.Properties.GetByIdAsync(propertyId);
            if (property == null)
            {
                return new ApiResponse<Property> { Success = false, Message = "Property not found" };
            }

            property.Status = PropertyStatus.SUSPENDED;
            property.IsActive = false;
            property.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync();

            return new ApiResponse<Property>
            {
                Success = true,
                Message = "Property suspended",
                Data = property
            };
        }

        public async Task<ApiResponse<Property>> UnsuspendPropertyAsync(string propertyId, string adminId, string? notes)
        {
            var property = await _unitOfWork.Properties.GetByIdAsync(propertyId);
            if (property == null)
            {
                return new ApiResponse<Property> { Success = false, Message = "Property not found" };
            }

            property.Status = PropertyStatus.ACTIVE;
            property.IsActive = true;
            property.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync();

            return new ApiResponse<Property>
            {
                Success = true,
                Message = "Property unsuspended",
                Data = property
            };
        }

        public async Task<ApiResponse<List<Property>>> GetPropertiesAsync(int page = 1, int limit = 20, string? status = null, string? hostId = null, string? searchQuery = null)
        {
            var skip = (page - 1) * limit;

            var query = _unitOfWork.Properties.Query().Include(p => p.Owner).AsQueryable();

            if (!string.IsNullOrEmpty(status) && Enum.TryParse<PropertyStatus>(status, true, out var propertyStatus))
            {
                query = query.Where(p => p.Status == propertyStatus);
            }

            if (!string.IsNullOrEmpty(hostId))
            {
                query = query.Where(p => p.OwnerId == hostId);
            }

            if (!string.IsNullOrEmpty(searchQuery))
            {
                query = query.Where(p =>
                    p.Title.Contains(searchQuery) ||
                    p.Description.Contains(searchQuery) ||
                    p.City.Contains(searchQuery));
            }

            var total = await query.CountAsync();
            var properties = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip(skip)
                .Take(limit)
                .ToListAsync();

            return new ApiResponse<List<Property>>
            {
                Success = true,
                Data = properties,
                Pagination = new PaginationDto
                {
                    Page = page,
                    Limit = limit,
                    Total = total,
                    TotalPages = (int)Math.Ceiling(total / (double)limit)
                }
            };
        }

        public async Task<ApiResponse<Property>> GetPropertyByIdAsync(string propertyId)
        {
            var property = await _unitOfWork.Properties.GetFullPropertyAsync(propertyId);

            if (property == null)
            {
                return new ApiResponse<Property> { Success = false, Message = "Property not found" };
            }

            return new ApiResponse<Property>
            {
                Success = true,
                Data = property
            };
        }
    }
}
