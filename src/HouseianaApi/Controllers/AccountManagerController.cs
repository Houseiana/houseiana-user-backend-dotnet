using Microsoft.AspNetCore.Mvc;
using HouseianaApi.Services;

namespace HouseianaApi.Controllers
{
    [ApiController]
    [Route("api/account-manager")]
    public class AccountManagerController : ControllerBase
    {
        private readonly AccountManagerService _accountService;

        public AccountManagerController(AccountManagerService accountService)
        {
            _accountService = accountService;
        }

        // GET /api/account-manager/overview
        [HttpGet("overview")]
        public async Task<IActionResult> GetOverview()
        {
            var result = await _accountService.GetOverviewAsync();
            return Ok(result);
        }

        // GET /api/account-manager/users
        [HttpGet("users")]
        public async Task<IActionResult> GetUsers(
            [FromQuery] int page = 1,
            [FromQuery] int limit = 20,
            [FromQuery] string? role = null,
            [FromQuery] string? status = null,
            [FromQuery] string? search = null)
        {
            var result = await _accountService.GetUsersAsync(page, limit, role, status, search);
            return Ok(result);
        }

        // GET /api/account-manager/users/{userId}
        [HttpGet("users/{userId}")]
        public async Task<IActionResult> GetUserById(string userId)
        {
            var result = await _accountService.GetUserByIdAsync(userId);
            if (!result.Success)
            {
                return NotFound(result);
            }
            return Ok(result);
        }

        // PATCH /api/account-manager/users/{userId}/status
        [HttpPatch("users/{userId}/status")]
        public async Task<IActionResult> UpdateUserStatus(string userId, [FromBody] UpdateUserStatusDto dto)
        {
            var result = await _accountService.UpdateUserStatusAsync(userId, dto.Status);
            if (!result.Success)
            {
                return NotFound(result);
            }
            return Ok(result);
        }

        // PATCH /api/account-manager/users/{userId}/kyc
        [HttpPatch("users/{userId}/kyc")]
        public async Task<IActionResult> UpdateKycStatus(string userId, [FromBody] UpdateKycDto dto)
        {
            var result = await _accountService.UpdateKycStatusAsync(userId, dto.KycStatus);
            if (!result.Success)
            {
                return NotFound(result);
            }
            return Ok(result);
        }
    }

    public class UpdateUserStatusDto
    {
        public string Status { get; set; } = string.Empty;
    }

    public class UpdateKycDto
    {
        public string KycStatus { get; set; } = string.Empty;
    }
}
