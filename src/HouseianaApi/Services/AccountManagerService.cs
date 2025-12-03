using Microsoft.EntityFrameworkCore;
using HouseianaApi.Data;
using HouseianaApi.DTOs;
using HouseianaApi.Enums;

namespace HouseianaApi.Services
{
    public class AccountManagerService
    {
        private readonly HouseianaDbContext _context;
        private readonly ILogger<AccountManagerService> _logger;

        public AccountManagerService(HouseianaDbContext context, ILogger<AccountManagerService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ApiResponse<AccountOverviewDto>> GetOverviewAsync()
        {
            var totalUsers = await _context.Users.CountAsync();
            var activeUsers = await _context.Users.CountAsync(u => u.AccountStatus == UserStatus.ACTIVE);
            var totalHosts = await _context.Users.CountAsync(u => u.IsHost);
            var totalGuests = await _context.Users.CountAsync(u => u.IsGuest);
            var pendingKYC = await _context.Users.CountAsync(u => u.KycStatus == KycStatus.PENDING);
            var totalRevenue = await _context.Bookings
                .Where(b => b.PaymentStatus == PaymentStatus.PAID)
                .SumAsync(b => b.TotalPrice);

            return new ApiResponse<AccountOverviewDto>
            {
                Success = true,
                Data = new AccountOverviewDto
                {
                    TotalUsers = totalUsers,
                    ActiveUsers = activeUsers,
                    TotalHosts = totalHosts,
                    TotalGuests = totalGuests,
                    PendingKYC = pendingKYC,
                    TotalRevenue = totalRevenue
                }
            };
        }

        public async Task<ApiResponse<List<Models.User>>> GetUsersAsync(
            int page = 1,
            int limit = 20,
            string? role = null,
            string? status = null,
            string? search = null)
        {
            var skip = (page - 1) * limit;
            var query = _context.Users.AsQueryable();

            if (!string.IsNullOrEmpty(role))
            {
                if (role == "host")
                    query = query.Where(u => u.IsHost);
                else if (role == "guest")
                    query = query.Where(u => u.IsGuest);
                else if (role == "admin")
                    query = query.Where(u => u.IsAdmin);
            }

            if (!string.IsNullOrEmpty(status) && Enum.TryParse<UserStatus>(status, true, out var userStatus))
            {
                query = query.Where(u => u.AccountStatus == userStatus);
            }

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(u =>
                    u.FirstName.Contains(search) ||
                    u.LastName.Contains(search) ||
                    u.Email.Contains(search));
            }

            var total = await query.CountAsync();
            var users = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip(skip)
                .Take(limit)
                .ToListAsync();

            return new ApiResponse<List<Models.User>>
            {
                Success = true,
                Data = users,
                Pagination = new PaginationDto
                {
                    Page = page,
                    Limit = limit,
                    Total = total,
                    TotalPages = (int)Math.Ceiling(total / (double)limit)
                }
            };
        }

        public async Task<ApiResponse<Models.User>> GetUserByIdAsync(string userId)
        {
            var user = await _context.Users
                .Include(u => u.Properties)
                .Include(u => u.GuestBookings)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return new ApiResponse<Models.User> { Success = false, Message = "User not found" };
            }

            return new ApiResponse<Models.User> { Success = true, Data = user };
        }

        public async Task<ApiResponse<Models.User>> UpdateUserStatusAsync(string userId, string status)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return new ApiResponse<Models.User> { Success = false, Message = "User not found" };
            }

            if (!Enum.TryParse<UserStatus>(status, true, out var newStatus))
            {
                return new ApiResponse<Models.User> { Success = false, Message = "Invalid user status" };
            }

            user.AccountStatus = newStatus;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return new ApiResponse<Models.User>
            {
                Success = true,
                Message = $"User status updated to {status}",
                Data = user
            };
        }

        public async Task<ApiResponse<Models.User>> UpdateKycStatusAsync(string userId, string kycStatus)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return new ApiResponse<Models.User> { Success = false, Message = "User not found" };
            }

            if (!Enum.TryParse<KycStatus>(kycStatus, true, out var newKycStatus))
            {
                return new ApiResponse<Models.User> { Success = false, Message = "Invalid KYC status" };
            }

            user.KycStatus = newKycStatus;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return new ApiResponse<Models.User>
            {
                Success = true,
                Message = $"KYC status updated to {kycStatus}",
                Data = user
            };
        }
    }
}
