using Microsoft.AspNetCore.Mvc;
using HouseianaApi.Services;

namespace HouseianaApi.Controllers
{
    [ApiController]
    [Route("api/properties")]
    public class PropertiesAdminController : ControllerBase
    {
        private readonly InventoryService _inventoryService;

        public PropertiesAdminController(InventoryService inventoryService)
        {
            _inventoryService = inventoryService;
        }

        // GET /api/properties
        [HttpGet]
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

        // GET /api/properties/{propertyId}
        [HttpGet("{propertyId}")]
        public async Task<IActionResult> GetPropertyById(string propertyId)
        {
            var result = await _inventoryService.GetPropertyByIdAsync(propertyId);
            if (!result.Success)
            {
                return NotFound(result);
            }
            return Ok(result);
        }
    }
}
