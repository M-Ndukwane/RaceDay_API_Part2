
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.Models;
using RaceDay.API.Services;

namespace RaceDay.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EventsController : ControllerBase
    {
        private const long MaxImageSize = 5 * 1024 * 1024;

        private static readonly string[] AllowedImageTypes =
        {
            "image/jpeg",
            "image/png",
            "image/webp"
        };

        private readonly RaceDayDbContext _context;
        private readonly BlobStorageService _blobStorageService;

        public EventsController(
            RaceDayDbContext context,
            BlobStorageService blobStorageService)
        {
            _context = context;
            _blobStorageService = blobStorageService;
        }

        private static bool IsValidImage(IFormFile image)
        {
            return image.Length > 0
                && image.Length <= MaxImageSize
                && AllowedImageTypes.Contains(
                    image.ContentType.ToLowerInvariant());
        }




        // GET: api/Events
        // Show events created by Organisers after initial testing.
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Event>>> GetEvents()
        {
            var events = await _context.Events
                .AsNoTracking()
                .Where(e => e.EventsID != 1 &&
                            e.EventsID != 2 &&
                            e.EventsID != 3 &&
                            e.EventsID != 4)
                .OrderBy(e => e.EventDate)
                .ToListAsync();

            return Ok(events);
        }


        // GET: api/Events/my-events
        // Organisers can view only events they created.
        [HttpGet("my-events")]
        [Authorize(Roles = "Organiser")]
        public async Task<ActionResult<IEnumerable<Event>>> GetMyEvents()
        {
            var userIdClaim = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdClaim, out int organiserId))
            {
                return Unauthorized(new
                {
                    message = "Invalid organiser identity."
                });
            }

            var events = await _context.Events
                .AsNoTracking()
                .Where(e => e.UserID == organiserId)
                .OrderBy(e => e.EventDate)
                .ToListAsync();

            return Ok(events);
        }


        // GET: api/Events/1
        [HttpGet("{id:int}")]
        public async Task<ActionResult<Event>> GetEvent(int id)
        {
            var eventItem = await _context.Events
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.EventsID == id);

            if (eventItem == null)
            {
                return NotFound(new
                {
                    message = "Event not found."
                });
            }

            return Ok(eventItem);
        }

        // POST: api/Events
        [Authorize(Roles = "Organiser")]
        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CreateEvent(
            [FromForm] CreateEventRequest request,
            IFormFile? image)
        {
            var currentUserId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (!int.TryParse(currentUserId, out int organiserId))
            {
                return Unauthorized();
            }

            var venueExists = await _context.Venues
                .AnyAsync(v => v.VenuesID == request.VenuesID);

            if (!venueExists)
            {
                return BadRequest(new
                {
                    message = "The selected venue does not exist."
                });
            }

            if (image != null && !IsValidImage(image))
            {
                return BadRequest(new
                {
                    message = "Invalid image. Upload a JPG, PNG, or WEBP image no larger than 5 MB."
                });
            }

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

            if (image != null)
            {
                using var imageStream = image.OpenReadStream();

                eventItem.ImageFileName =
                    await _blobStorageService.UploadImageAsync(
                        imageStream,
                        image.FileName,
                        image.ContentType);
            }

            _context.Events.Add(eventItem);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch
            {
                if (!string.IsNullOrWhiteSpace(
                    eventItem.ImageFileName))
                {
                    try
                    {
                        await _blobStorageService.DeleteImageAsync(
                            eventItem.ImageFileName);
                    }
                    catch
                    {
                        // Image cleanup can be retried later.
                    }
                }

                throw;
            }

            return CreatedAtAction(
                nameof(GetEvent),
                new { id = eventItem.EventsID },
                new
                {
                    eventItem.EventsID,
                    eventItem.EventName,
                    eventItem.Description,
                    eventItem.ImageFileName,
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
        [HttpPut("{id:int}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdateEvent(
            int id,
            [FromForm] UpdateEventRequest request,
            IFormFile? image)
        {
            var currentUserId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (!int.TryParse(currentUserId, out int organiserId))
            {
                return Unauthorized();
            }

            var eventItem = await _context.Events
                .FirstOrDefaultAsync(e => e.EventsID == id);

            if (eventItem == null)
            {
                return NotFound(new
                {
                    message = "Event not found."
                });
            }

            // Prevent organisers from editing other organisers' events.
            if (eventItem.UserID != organiserId)
            {
                return Forbid();
            }

            var venueExists = await _context.Venues
                .AnyAsync(v => v.VenuesID == request.VenuesID);

            if (!venueExists)
            {
                return BadRequest(new
                {
                    message = "The selected venue does not exist."
                });
            }

            if (image != null && !IsValidImage(image))
            {
                return BadRequest(new
                {
                    message = "Invalid image. Upload a JPG, PNG, or WEBP image no larger than 5 MB."
                });
            }

            string? oldImageFileName = eventItem.ImageFileName;
            string? newImageFileName = null;

            if (image != null)
            {
                using var imageStream = image.OpenReadStream();

                newImageFileName =
                    await _blobStorageService.UploadImageAsync(
                        imageStream,
                        image.FileName,
                        image.ContentType);

                eventItem.ImageFileName = newImageFileName;
            }

            eventItem.EventName = request.EventName;
            eventItem.Description = request.Description;
            eventItem.EventDate = request.EventDate;
            eventItem.StartTime = request.StartTime;
            eventItem.RegistrationDeadline =
                request.RegistrationDeadline;
            eventItem.VenuesID = request.VenuesID;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch
            {
                if (!string.IsNullOrWhiteSpace(newImageFileName))
                {
                    try
                    {
                        await _blobStorageService.DeleteImageAsync(
                            newImageFileName);
                    }
                    catch
                    {
                        // Image cleanup can be retried later.
                    }
                }

                throw;
            }

            // Delete the previous image only after saving succeeds.
            if (newImageFileName != null &&
                !string.IsNullOrWhiteSpace(oldImageFileName))
            {
                try
                {
                    await _blobStorageService.DeleteImageAsync(
                        oldImageFileName);
                }
                catch
                {
                    // Event update succeeded.
                    // Old image cleanup can be retried later.
                }
            }

            return NoContent();
        }

        // DELETE: api/Events/1
        [Authorize(Roles = "Organiser")]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            var currentUserId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (!int.TryParse(currentUserId, out int organiserId))
            {
                return Unauthorized();
            }

            var eventItem = await _context.Events
                .FirstOrDefaultAsync(e => e.EventsID == id);

            if (eventItem == null)
            {
                return NotFound(new
                {
                    message = "Event not found."
                });
            }

            // Prevent organisers from deleting other organisers' events.
            if (eventItem.UserID != organiserId)
            {
                return Forbid();
            }

            string? imageFileName = eventItem.ImageFileName;

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

            // Delete the Azure image after the database record is deleted.
            if (!string.IsNullOrWhiteSpace(imageFileName))
            {
                try
                {
                    await _blobStorageService.DeleteImageAsync(
                        imageFileName);
                }
                catch
                {
                    // Event deletion succeeded.
                    // Image cleanup can be retried later.
                }
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
