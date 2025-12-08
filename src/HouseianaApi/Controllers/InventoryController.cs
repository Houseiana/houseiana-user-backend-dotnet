using Microsoft.AspNetCore.Mvc;
using Houseiana.Business;
using Houseiana.DTOs;

namespace HouseianaApi.Controllers
{
    [ApiController]
    [Route("inventory")]
    public class InventoryController : ControllerBase
    {
        private readonly InventoryService _inventoryService;

        public InventoryController(InventoryService inventoryService)
        {
            _inventoryService = inventoryService;
        }

        // POST /inventory/auth/login
        [HttpPost("auth/login")]
        public async Task<IActionResult> Login([FromBody] AdminLoginDto dto)
        {
            var result = await _inventoryService.LoginAsync(dto.Email, dto.Password);
            if (!result.Success)
            {
                return Unauthorized(new { message = "Invalid credentials" });
            }
            return Ok(result);
        }

        // GET /inventory/dashboard/kpis
        [HttpGet("dashboard/kpis")]
        public async Task<IActionResult> GetDashboardKPIs([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            var result = await _inventoryService.GetDashboardKPIsAsync(startDate, endDate);
            return Ok(result);
        }

        // GET /inventory/approvals/pending
        [HttpGet("approvals/pending")]
        public async Task<IActionResult> GetPendingApprovals(
            [FromQuery] int page = 1,
            [FromQuery] int limit = 20,
            [FromQuery] string sortBy = "createdAt",
            [FromQuery] string sortOrder = "desc")
        {
            var result = await _inventoryService.GetPendingApprovalsAsync(page, limit, sortBy, sortOrder);
            return Ok(result);
        }

        // GET /inventory/properties
        [HttpGet("properties")]
        public async Task<IActionResult> GetProperties(
            [FromQuery] int page = 1,
            [FromQuery] int limit = 20,
            [FromQuery] string? status = null,
            [FromQuery] string? hostId = null,
            [FromQuery] string? searchQuery = null)
        {
            var result = await _inventoryService.GetPropertiesAsync(page, limit, status, hostId, searchQuery);
            return Ok(result);
        }

        // GET /inventory/properties/{propertyId}
        [HttpGet("properties/{propertyId}")]
        public async Task<IActionResult> GetPropertyById(string propertyId)
        {
            var result = await _inventoryService.GetPropertyByIdAsync(propertyId);
            if (!result.Success)
            {
                return NotFound(result);
            }
            return Ok(result);
        }

        // POST /inventory/properties/{propertyId}/approve
        [HttpPost("properties/{propertyId}/approve")]
        public async Task<IActionResult> ApproveProperty(string propertyId, [FromBody] ApprovePropertyDto dto)
        {
            var result = await _inventoryService.ApprovePropertyAsync(propertyId, dto.AdminId, dto.Notes);
            if (!result.Success)
            {
                return NotFound(result);
            }
            return Ok(result);
        }

        // POST /inventory/properties/{propertyId}/reject
        [HttpPost("properties/{propertyId}/reject")]
        public async Task<IActionResult> RejectProperty(string propertyId, [FromBody] RejectPropertyDto dto)
        {
            var result = await _inventoryService.RejectPropertyAsync(propertyId, dto.AdminId, dto.Reason, dto.ChangesRequested);
            if (!result.Success)
            {
                return NotFound(result);
            }
            return Ok(result);
        }

        // POST /inventory/properties/{propertyId}/suspend
        [HttpPost("properties/{propertyId}/suspend")]
        public async Task<IActionResult> SuspendProperty(string propertyId, [FromBody] SuspendPropertyDto dto)
        {
            var result = await _inventoryService.SuspendPropertyAsync(propertyId, dto.AdminId, dto.Reason);
            if (!result.Success)
            {
                return NotFound(result);
            }
            return Ok(result);
        }

        // POST /inventory/properties/{propertyId}/unsuspend
        [HttpPost("properties/{propertyId}/unsuspend")]
        public async Task<IActionResult> UnsuspendProperty(string propertyId, [FromBody] UnsuspendPropertyDto dto)
        {
            var result = await _inventoryService.UnsuspendPropertyAsync(propertyId, dto.AdminId, dto.Notes);
            if (!result.Success)
            {
                return NotFound(result);
            }
            return Ok(result);
        }
    }
}
