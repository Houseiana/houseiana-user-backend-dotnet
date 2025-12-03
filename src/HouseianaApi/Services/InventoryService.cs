using Microsoft.EntityFrameworkCore;
using HouseianaApi.Data;
using HouseianaApi.Models;
using HouseianaApi.DTOs;
using HouseianaApi.Enums;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace HouseianaApi.Services
{
    public class InventoryService
    {
        private readonly HouseianaDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<InventoryService> _logger;

        public InventoryService(HouseianaDbContext context, IConfiguration configuration, ILogger<InventoryService> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<AdminLoginResponseDto> LoginAsync(string email, string password)
        {
            var admin = await _context.Admins
                .FirstOrDefaultAsync(a => (a.Email == email || a.Username == email) && a.IsActive);

            if (admin == null)
            {
                return new AdminLoginResponseDto { Success = false };
            }

            // Simple password comparison (in production, use proper hashing)
            var isValid = BCrypt.Net.BCrypt.Verify(password, admin.Password);
            if (!isValid)
            {
                return new AdminLoginResponseDto { Success = false };
            }

            // Update last login
            admin.LastLogin = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Generate JWT token
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

            var totalProperties = await _context.Properties.CountAsync();
            var activeProperties = await _context.Properties.CountAsync(p => p.Status == PropertyStatus.ACTIVE && p.IsActive);
            var suspendedProperties = await _context.Properties.CountAsync(p => p.Status == PropertyStatus.SUSPENDED);
            var pendingApprovals = await _context.Properties.CountAsync(p => p.Status == PropertyStatus.PENDING || p.Status == PropertyStatus.DRAFT);
            var totalHosts = await _context.Users.CountAsync(u => u.IsHost);
            var avgRating = await _context.Properties.AverageAsync(p => (double?)p.AverageRating) ?? 0;

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

            var query = _context.Properties
                .Include(p => p.Owner)
                .Where(p => p.Status == PropertyStatus.PENDING || p.Status == PropertyStatus.DRAFT);

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

        public async Task<ApiResponse<Property>> ApprovePropertyAsync(string propertyId, string adminId, string? notes)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var property = await _context.Properties.FindAsync(propertyId);
                if (property == null)
                {
                    return new ApiResponse<Property> { Success = false, Message = "Property not found" };
                }

                property.Status = PropertyStatus.ACTIVE;
                property.IsActive = true;
                property.UpdatedAt = DateTime.UtcNow;

                // Create approval record
                var approval = new PropertyApproval
                {
                    PropertyId = propertyId,
                    AdminId = adminId,
                    Status = "APPROVED",
                    Comments = notes
                };
                _context.PropertyApprovals.Add(approval);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return new ApiResponse<Property>
                {
                    Success = true,
                    Message = "Property approved successfully",
                    Data = property
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error approving property {PropertyId}", propertyId);
                throw;
            }
        }

        public async Task<ApiResponse<Property>> RejectPropertyAsync(string propertyId, string adminId, string reason, List<string>? changesRequested)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var property = await _context.Properties.FindAsync(propertyId);
                if (property == null)
                {
                    return new ApiResponse<Property> { Success = false, Message = "Property not found" };
                }

                property.Status = PropertyStatus.REJECTED;
                property.UpdatedAt = DateTime.UtcNow;

                // Create approval record
                var approval = new PropertyApproval
                {
                    PropertyId = propertyId,
                    AdminId = adminId,
                    Status = "REJECTED",
                    Comments = reason,
                    Changes = changesRequested != null ? System.Text.Json.JsonSerializer.Serialize(changesRequested) : null
                };
                _context.PropertyApprovals.Add(approval);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return new ApiResponse<Property>
                {
                    Success = true,
                    Message = "Property rejected",
                    Data = property
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error rejecting property {PropertyId}", propertyId);
                throw;
            }
        }

        public async Task<ApiResponse<Property>> SuspendPropertyAsync(string propertyId, string adminId, string reason)
        {
            var property = await _context.Properties.FindAsync(propertyId);
            if (property == null)
            {
                return new ApiResponse<Property> { Success = false, Message = "Property not found" };
            }

            property.Status = PropertyStatus.SUSPENDED;
            property.IsActive = false;
            property.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new ApiResponse<Property>
            {
                Success = true,
                Message = "Property suspended",
                Data = property
            };
        }

        public async Task<ApiResponse<Property>> UnsuspendPropertyAsync(string propertyId, string adminId, string? notes)
        {
            var property = await _context.Properties.FindAsync(propertyId);
            if (property == null)
            {
                return new ApiResponse<Property> { Success = false, Message = "Property not found" };
            }

            property.Status = PropertyStatus.ACTIVE;
            property.IsActive = true;
            property.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

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

            var query = _context.Properties.Include(p => p.Owner).AsQueryable();

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
            var property = await _context.Properties
                .Include(p => p.Owner)
                .Include(p => p.PropertyCalendars)
                .FirstOrDefaultAsync(p => p.Id == propertyId);

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
