
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;


namespace RaceDay.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly RaceDayDbContext _context;

        public UsersController(RaceDayDbContext context)
        {
            _context = context;
        }

        // GET: api/Users/5

        [Authorize]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser(int id)
        {
            var currentUserId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            var isOrganiser = User.IsInRole("Organiser");

            if (!int.TryParse(currentUserId, out var authenticatedUserId))
            {
                return Unauthorized();
            }

            if (authenticatedUserId != id && !isOrganiser)
            {
                return Forbid();
            }

            var user = await _context.Users
                .Where(u => u.UsersID == id)
                .Select(u => new
                {
                    u.UsersID,
                    u.FirstName,
                    u.LastName,
                    u.Email,
                    u.PhoneNumber,
                    u.CreatedAt,
                    u.IsActive,
                    u.RolesID
                })
                .FirstOrDefaultAsync();

            if (user == null)
            {
                return NotFound();
            }

            return Ok(user);
        }
    }
}
