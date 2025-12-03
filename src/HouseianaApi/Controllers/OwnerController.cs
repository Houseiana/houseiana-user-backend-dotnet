using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HouseianaApi.Data;

namespace HouseianaApi.Controllers
{
    [ApiController]
    [Route("api/owner")]
    public class OwnerController : ControllerBase
    {
        private readonly HouseianaDbContext _context;

        public OwnerController(HouseianaDbContext context)
        {
            _context = context;
        }

        // GET /api/owner/admins
        [HttpGet("admins")]
        public async Task<IActionResult> GetAdmins()
        {
            var admins = await _context.Admins
                .Where(a => a.IsActive)
                .Select(a => new
                {
                    a.Id,
                    a.Username,
                    a.Email,
                    a.FullName,
                    a.Role,
                    a.LastLogin,
                    a.CreatedAt
                })
                .ToListAsync();

            return Ok(new { success = true, data = admins });
        }
    }
}
