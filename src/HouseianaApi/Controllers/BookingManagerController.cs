using Microsoft.AspNetCore.Mvc;
using Houseiana.Business;
using Houseiana.DTOs;

namespace HouseianaApi.Controllers;

[ApiController]
[Route("booking-manager")]
[Produces("application/json")]
public class BookingManagerController : ControllerBase
{
    private readonly IBookingManagerService _bookingManagerService;
    private readonly ILogger<BookingManagerController> _logger;

    public BookingManagerController(IBookingManagerService bookingManagerService, ILogger<BookingManagerController> logger)
    {
        _bookingManagerService = bookingManagerService;
        _logger = logger;
    }

    /// <summary>
    /// Create a new booking with availability locking
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateBooking([FromBody] CreateBookingDto dto)
    {
        try
        {
            var result = await _bookingManagerService.CreateBookingAsync(dto);
            return StatusCode(StatusCodes.Status201Created, result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Confirm booking after payment (converts soft-hold to confirmed)
    /// </summary>
    [HttpPost("{id}/confirm")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ConfirmBooking(string id)
    {
        try
        {
            var result = await _bookingManagerService.ConfirmBookingAsync(id);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Approve booking (host approval)
    /// </summary>
    [HttpPost("{id}/approve")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ApproveBooking(string id, [FromBody] ApproveRejectBookingDto dto)
    {
        try
        {
            var result = await _bookingManagerService.ApproveBookingAsync(id, dto.HostId);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Reject booking (host rejection)
    /// </summary>
    [HttpPost("{id}/reject")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RejectBooking(string id, [FromBody] ApproveRejectBookingDto dto)
    {
        try
        {
            var result = await _bookingManagerService.RejectBookingAsync(id, dto.HostId, dto.Reason);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Cancel booking
    /// </summary>
    [HttpPost("{id}/cancel")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelBooking(string id, [FromBody] CancelBookingDto dto)
    {
        try
        {
            var result = await _bookingManagerService.CancelBookingAsync(id, dto.UserId, dto.Reason);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get booking by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBooking(string id)
    {
        try
        {
            var result = await _bookingManagerService.GetBookingAsync(id);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get bookings for a user
    /// </summary>
    [HttpGet("user/{userId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserBookings(string userId, [FromQuery] string role = "guest")
    {
        var result = await _bookingManagerService.GetUserBookingsAsync(userId, role);
        return Ok(result);
    }
}
