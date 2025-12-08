using Houseiana.DTOs;
using Houseiana.Enums;
using Houseiana.Repositories;
using Microsoft.EntityFrameworkCore;
using Houseiana.DAL.Models;
using Microsoft.Extensions.Logging;

namespace Houseiana.Business
{
    public class AccountManagerService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<AccountManagerService> _logger;

        public AccountManagerService(IUnitOfWork unitOfWork, ILogger<AccountManagerService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<ApiResponse<AccountOverviewDto>> GetOverviewAsync()
        {
            var totalUsers = await _unitOfWork.Users.CountAsync();
            var activeUsers = await _unitOfWork.Users.CountAsync(u => u.AccountStatus == UserStatus.ACTIVE);
            var totalHosts = await _unitOfWork.Users.CountAsync(u => u.IsHost);
            var totalGuests = await _unitOfWork.Users.CountAsync(u => u.IsGuest);
            var pendingKYC = await _unitOfWork.Users.CountAsync(u => u.KycStatus == KycStatus.PENDING);

            var paidBookings = await _unitOfWork.Bookings.FindAsync(b => b.PaymentStatus == PaymentStatus.PAID);
            var totalRevenue = paidBookings.Sum(b => b.TotalPrice);

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

        public async Task<ApiResponse<List<User>>> GetUsersAsync(
            int page = 1,
            int limit = 20,
            string? role = null,
            string? status = null,
            string? search = null)
        {
            var skip = (page - 1) * limit;
            var query = _unitOfWork.Users.Query();

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

            return new ApiResponse<List<User>>
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

        public async Task<ApiResponse<User>> GetUserByIdAsync(string userId)
        {
            var user = await _unitOfWork.Users.GetWithPropertiesAsync(userId);

            if (user == null)
            {
                return new ApiResponse<User> { Success = false, Message = "User not found" };
            }

            return new ApiResponse<User> { Success = true, Data = user };
        }

        public async Task<ApiResponse<User>> UpdateUserStatusAsync(string userId, string status)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            if (user == null)
            {
                return new ApiResponse<User> { Success = false, Message = "User not found" };
            }

            if (!Enum.TryParse<UserStatus>(status, true, out var newStatus))
            {
                return new ApiResponse<User> { Success = false, Message = "Invalid user status" };
            }

            user.AccountStatus = newStatus;
            user.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.SaveChangesAsync();

            return new ApiResponse<User>
            {
                Success = true,
                Message = $"User status updated to {status}",
                Data = user
            };
        }

        public async Task<ApiResponse<User>> UpdateKycStatusAsync(string userId, string kycStatus)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            if (user == null)
            {
                return new ApiResponse<User> { Success = false, Message = "User not found" };
            }

            if (!Enum.TryParse<KycStatus>(kycStatus, true, out var newKycStatus))
            {
                return new ApiResponse<User> { Success = false, Message = "Invalid KYC status" };
            }

            user.KycStatus = newKycStatus;
            user.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.SaveChangesAsync();

            return new ApiResponse<User>
            {
                Success = true,
                Message = $"KYC status updated to {kycStatus}",
                Data = user
            };
        }
    }
}
