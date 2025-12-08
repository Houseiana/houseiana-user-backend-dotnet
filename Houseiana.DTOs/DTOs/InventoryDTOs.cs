namespace Houseiana.DTOs
{
    // Login
    public class AdminLoginDto
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class AdminLoginResponseDto
    {
        public bool Success { get; set; }
        public string? Token { get; set; }
        public AdminUserDto? User { get; set; }
    }

    public class AdminUserDto
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }

    // Property Approval
    public class ApprovePropertyDto
    {
        public string AdminId { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }

    public class RejectPropertyDto
    {
        public string AdminId { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public List<string>? ChangesRequested { get; set; }
    }

    public class SuspendPropertyDto
    {
        public string AdminId { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
    }

    public class UnsuspendPropertyDto
    {
        public string AdminId { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }

    public class SoftDeletePropertyDto
    {
        public string AdminId { get; set; } = string.Empty;
        public string? Reason { get; set; }
    }

    public class RestorePropertyDto
    {
        public string AdminId { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }

    // Dashboard KPIs
    public class DashboardKPIsDto
    {
        public int TotalProperties { get; set; }
        public int ActiveProperties { get; set; }
        public int SuspendedProperties { get; set; }
        public int PendingApprovals { get; set; }
        public int TotalHosts { get; set; }
        public double TotalRevenue { get; set; }
        public double RevenueChange { get; set; }
        public double OccupancyRate { get; set; }
        public double OccupancyChange { get; set; }
        public double AverageRating { get; set; }
        public double RatingChange { get; set; }
        public double HostsChange { get; set; }
        public string Currency { get; set; } = "USD";
        public PeriodDto? Period { get; set; }
    }

    public class PeriodDto
    {
        public string StartDate { get; set; } = string.Empty;
        public string EndDate { get; set; } = string.Empty;
    }

    // Pagination
    public class PaginationDto
    {
        public int Page { get; set; }
        public int Limit { get; set; }
        public int Total { get; set; }
        public int TotalPages { get; set; }
    }

    // Generic Response
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public T? Data { get; set; }
        public PaginationDto? Pagination { get; set; }
    }

    // Account Manager
    public class AccountOverviewDto
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int TotalHosts { get; set; }
        public int TotalGuests { get; set; }
        public int PendingKYC { get; set; }
        public double TotalRevenue { get; set; }
    }

    // Supervisor Stats
    public class SupervisorStatsDto
    {
        public int TotalBookings { get; set; }
        public int PendingBookings { get; set; }
        public int ConfirmedBookings { get; set; }
        public int CompletedBookings { get; set; }
        public int CancelledBookings { get; set; }
        public double TotalRevenue { get; set; }
        public int TotalProperties { get; set; }
        public int ActiveProperties { get; set; }
    }
}
