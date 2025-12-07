using Microsoft.AspNetCore.Mvc;
using HouseianaApi.Services;

namespace HouseianaApi.Controllers;

[ApiController]
[Route("users")]
[Produces("application/json")]
public class UsersController : ControllerBase
{
    private readonly IUsersService _usersService;

    public UsersController(IUsersService usersService)
    {
        _usersService = usersService;
    }

    /// <summary>
    /// Get user by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUser(string id)
    {
        var user = await _usersService.GetUserByIdAsync(id);

        if (user == null)
        {
            return NotFound(new { error = "User not found" });
        }

        return Ok(user);
    }

    /// <summary>
    /// Create Sadad payment - returns form data to submit to Sadad
    /// </summary>
    [HttpPost("sadad/payment")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateSadadPayment([FromBody] SadadPaymentRequest request)
    {
        if (request.Amount <= 0)
        {
            return BadRequest(new { error = "Amount must be greater than 0" });
        }

        if (string.IsNullOrEmpty(request.OrderId))
        {
            return BadRequest(new { error = "OrderId is required" });
        }

        var response = await _usersService.GetSadadPayment(request);

        if (!response.Success)
        {
            return BadRequest(new { error = response.Error });
        }

        return Ok(response);
    }
}