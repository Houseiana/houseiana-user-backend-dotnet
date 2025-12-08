using Microsoft.AspNetCore.Mvc;
using Houseiana.Business;

namespace HouseianaApi.Controllers
{
    [ApiController]
    [Route("api/bookings")]
    public class BookingsAdminController : ControllerBase
    {
        private readonly BookingsAdminService _bookingsService;

        public BookingsAdminController(BookingsAdminService bookingsService)
        {
            _bookingsService = bookingsService;
        }

        // GET /api/bookings
        [HttpGet]
        public async Task<IActionResult> GetBookings(
            [FromQuery] int page = 1,
            [FromQuery] int limit = 20,
            [FromQuery] string? status = null,
            [FromQuery] string? guestId = null,
            [FromQuery] string? hostId = null,
            [FromQuery] string? propertyId = null)
        {
            var result = await _bookingsService.GetBookingsAsync(page, limit, status, guestId, hostId, propertyId);
            return Ok(result);
        }

        // GET /api/bookings/{bookingId}
        [HttpGet("{bookingId}")]
        public async Task<IActionResult> GetBookingById(string bookingId)
        {
            var result = await _bookingsService.GetBookingByIdAsync(bookingId);
            if (!result.Success)
            {
                return NotFound(result);
            }
            return Ok(result);
        }

        // PATCH /api/bookings/{bookingId}/status
        [HttpPatch("{bookingId}/status")]
        public async Task<IActionResult> UpdateBookingStatus(string bookingId, [FromBody] UpdateStatusDto dto)
        {
            var result = await _bookingsService.UpdateBookingStatusAsync(bookingId, dto.Status, dto.Reason);
            if (!result.Success)
            {
                return NotFound(result);
            }
            return Ok(result);
        }
    }

    public class UpdateStatusDto
    {
        public string Status { get; set; } = string.Empty;
        public string? Reason { get; set; }
    }
}
