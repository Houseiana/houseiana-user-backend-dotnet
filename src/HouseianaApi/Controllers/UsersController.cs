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
}
