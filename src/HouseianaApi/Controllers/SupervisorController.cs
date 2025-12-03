using Microsoft.AspNetCore.Mvc;
using HouseianaApi.Services;

namespace HouseianaApi.Controllers
{
    [ApiController]
    [Route("api/supervisor")]
    public class SupervisorController : ControllerBase
    {
        private readonly BookingsAdminService _bookingsService;

        public SupervisorController(BookingsAdminService bookingsService)
        {
            _bookingsService = bookingsService;
        }

        // GET /api/supervisor/stats
        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            var stats = await _bookingsService.GetStatsAsync();
            return Ok(new { success = true, data = stats });
        }
    }
}
