

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EventsController : ControllerBase
    {
        private readonly RaceDayDbContext _context;

        public EventsController(RaceDayDbContext context)
        {
            _context = context;
        }

        // GET: api/Events
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Event>>> GetEvents()
        {
            var events = await _context.Events
                .AsNoTracking()
                .ToListAsync();

            return Ok(events);
        }

        // GET: api/Events/1
        [HttpGet("{id}")]
        public async Task<ActionResult<Event>> GetEvent(int id)
        {
            var eventItem = await _context.Events
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.EventsID == id);

            if (eventItem == null)
                return NotFound(new { message = "Event not found." });

            return Ok(eventItem);
        }

        // POST: api/Events
        [Authorize(Roles = "Organiser")]
        [HttpPost]
        public async Task<ActionResult> CreateEvent(CreateEventRequest request)
        {
            var userIdValue = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdValue, out int organiserId))
                return Unauthorized();

            var venueExists = await _context.Venues
                .AnyAsync(v => v.VenuesID == request.VenuesID);

            if (!venueExists)
                return BadRequest(new { message = "The selected venue does not exist." });

            var eventItem = new Event
            {
                EventName = request.EventName,
                Description = request.Description,
                EventDate = request.EventDate,
                StartTime = request.StartTime,
                RegistrationDeadline = request.RegistrationDeadline,
                Status = "Open",
                CreatedAt = DateTime.UtcNow,
                UserID = organiserId,
                VenuesID = request.VenuesID
            };

            _context.Events.Add(eventItem);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetEvent),
                new { id = eventItem.EventsID },
                new
                {
                    eventItem.EventsID,
                    eventItem.EventName,
                    eventItem.Description,
                    eventItem.EventDate,
                    eventItem.StartTime,
                    eventItem.RegistrationDeadline,
                    eventItem.Status,
                    eventItem.UserID,
                    eventItem.VenuesID
                });
        }

        // PUT: api/Events/1
        [Authorize(Roles = "Organiser")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateEvent(
            int id, UpdateEventRequest request)
        {
            var eventItem = await _context.Events
                .FirstOrDefaultAsync(e => e.EventsID == id);

            if (eventItem == null)
                return NotFound(new { message = "Event not found." });

            var venueExists = await _context.Venues
                .AnyAsync(v => v.VenuesID == request.VenuesID);

            if (!venueExists)
                return BadRequest(new { message = "The selected venue does not exist." });

            eventItem.EventName = request.EventName;
            eventItem.Description = request.Description;
            eventItem.EventDate = request.EventDate;
            eventItem.StartTime = request.StartTime;
            eventItem.RegistrationDeadline = request.RegistrationDeadline;
            eventItem.VenuesID = request.VenuesID;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/Events/1
        [Authorize(Roles = "Organiser")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            var eventItem = await _context.Events
                .FirstOrDefaultAsync(e => e.EventsID == id);

            if (eventItem == null)
                return NotFound(new { message = "Event not found." });

            _context.Events.Remove(eventItem);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return Conflict(new
                {
                    message = "This event has related records and cannot be deleted. Remove or resolve the related records first."
                });
            }

            return NoContent();
        }
    }

    public class CreateEventRequest
    {
        public string EventName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime EventDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public DateTime RegistrationDeadline { get; set; }
        public int VenuesID { get; set; }
    }

    public class UpdateEventRequest
    {
        public string EventName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime EventDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public DateTime RegistrationDeadline { get; set; }
        public int VenuesID { get; set; }
    }
}
