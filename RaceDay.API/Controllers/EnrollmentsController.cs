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
    [Authorize]
    public class EnrollmentsController : ControllerBase
    {
        private readonly RaceDayDbContext _context;

        public EnrollmentsController(RaceDayDbContext context)
        {
            _context = context;
        }

        // GET: api/Enrollments
        // Organisers can view all enrollments.
        // Participants can view only their own enrollments.
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Enrollment>>> GetEnrollments()
        {
            var userIdClaim = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new
                {
                    message = "Invalid user identity."
                });
            }

            var query = _context.Enrollments
                .AsNoTracking();

            if (!User.IsInRole("Organiser"))
            {
                query = query.Where(e => e.UsersID == userId);
            }

            var enrollments = await query.ToListAsync();

            return Ok(enrollments);
        }

        // GET: api/Enrollments/5
        // Participants can view their own enrollment.
        // Organisers can view any enrollment.
        [HttpGet("{id:int}")]
        public async Task<ActionResult<Enrollment>> GetEnrollment(int id)
        {
            var enrollment = await _context.Enrollments
                .AsNoTracking()
                .FirstOrDefaultAsync(e =>
                    e.EnrollmentsID == id);

            if (enrollment == null)
            {
                return NotFound(new
                {
                    message = "Enrollment not found."
                });
            }

            var userIdClaim = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new
                {
                    message = "Invalid user identity."
                });
            }

            if (!User.IsInRole("Organiser") &&
                enrollment.UsersID != userId)
            {
                return Forbid();
            }

            return Ok(enrollment);
        }

        // POST: api/Enrollments
        // Participants can register themselves for an event category.
        [HttpPost]
        [Authorize(Roles = "Participant")]
        public async Task<ActionResult<Enrollment>> CreateEnrollment(
            CreateEnrollmentRequest request)
        {
            var userIdClaim = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new
                {
                    message = "Invalid user identity."
                });
            }

            // Validate that the category belongs to the specified event.
            var category = await _context.Categories
                .AsNoTracking()
                .FirstOrDefaultAsync(c =>
                    c.CategoriesID == request.CategoriesID &&
                    c.EventsID == request.EventsID);

            if (category == null)
            {
                return BadRequest(new
                {
                    message = "The selected category does not belong to the specified event."
                });
            }

            // Find the event.
            var eventItem = await _context.Events
                .AsNoTracking()
                .FirstOrDefaultAsync(e =>
                    e.EventsID == request.EventsID);

            if (eventItem == null)
            {
                return NotFound(new
                {
                    message = "Event not found."
                });
            }

            // Check whether the event is open.
            if (!string.Equals(
                eventItem.Status,
                "Open",
                StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new
                {
                    message = "This event is not open for registration."
                });
            }

            // Check the registration deadline.
            if (eventItem.RegistrationDeadline < DateTime.Now)
            {
                return BadRequest(new
                {
                    message = "The registration deadline has passed."
                });
            }

            // Prevent duplicate enrollment in the same category.
            var alreadyEnrolled = await _context.Enrollments
                .AnyAsync(e =>
                    e.UsersID == userId &&
                    e.CategoriesID == request.CategoriesID);

            if (alreadyEnrolled)
            {
                return Conflict(new
                {
                    message = "You are already enrolled in this category."
                });
            }

            // Check the category's participant limit.
            var currentEnrollments = await _context.Enrollments
                .CountAsync(e =>
                    e.CategoriesID == request.CategoriesID &&
                    e.Status == "Confirmed");

            if (currentEnrollments >= category.MaxParticipants)
            {
                return Conflict(new
                {
                    message = "This category has reached its participant limit."
                });
            }

            // Create the enrollment using the authenticated user's ID.
            var enrollment = new Enrollment
            {
                EnrolledAt = DateTime.Now,
                Status = "Confirmed",
                EmergencyContactName =
                    request.EmergencyContactName,
                EmergencyContactPhone =
                    request.EmergencyContactPhone,
                CategoriesID = request.CategoriesID,
                EventsID = request.EventsID,
                UsersID = userId
            };

            _context.Enrollments.Add(enrollment);

            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetEnrollment),
                new { id = enrollment.EnrollmentsID },
                enrollment);
        }
    }

    // Request body for POST: api/Enrollments
    public class CreateEnrollmentRequest
    {
        public int CategoriesID { get; set; }

        public int EventsID { get; set; }

        public string? EmergencyContactName { get; set; }

        public string? EmergencyContactPhone { get; set; }
    }
}