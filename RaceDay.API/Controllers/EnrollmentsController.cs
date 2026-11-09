using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.Models;
using Microsoft.Data.SqlClient;

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
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { message = "Invalid user identity." });
            }

            var query = _context.Enrollments.AsNoTracking();

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
                .FirstOrDefaultAsync(e => e.EnrollmentsID == id);

            if (enrollment == null)
            {
                return NotFound(new { message = "Enrollment not found." });
            }

            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { message = "Invalid user identity." });
            }

            if (!User.IsInRole("Organiser") && enrollment.UsersID != userId)
            {
                return Forbid();
            }

            return Ok(enrollment);
        }

        // GET: api/Enrollments/organiser
        // Organisers can view all enrollments with participant and event details.
        [HttpGet("organiser")]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> GetOrganiserEnrollments()
        {
            var enrollments = await _context.Enrollments
                .AsNoTracking()
                .Select(e => new
                {
                    e.EnrollmentsID,
                    e.UsersID,
                    e.EventsID,
                    e.CategoriesID,
                    e.EnrolledAt,
                    e.Status,
                    e.EmergencyContactName,
                    e.EmergencyContactPhone,
                    ParticipantName = e.Participant.FirstName + " " + e.Participant.LastName,
                    ParticipantEmail = e.Participant.Email,
                    EventName = e.Event.EventName,
                    CategoryName = e.Category.CategoryName,
                    EntryFee = e.Category.EntryFee
                })
                .ToListAsync();

            return Ok(enrollments);
        }

        // POST: api/Enrollments
        // Participants can register themselves for an event category.
        [HttpPost]
        [Authorize(Roles = "Participant")]
        public async Task<ActionResult<Enrollment>> CreateEnrollment(CreateEnrollmentRequest request)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { message = "Invalid user identity." });
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
                .FirstOrDefaultAsync(e => e.EventsID == request.EventsID);

            if (eventItem == null)
            {
                return NotFound(new { message = "Event not found." });
            }

            // Check whether the event is open.
            if (!string.Equals(eventItem.Status, "Open", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { message = "This event is not open for registration." });
            }

            // Check the registration deadline.
            if (eventItem.RegistrationDeadline < DateTime.Now)
            {
                return BadRequest(new { message = "The registration deadline has passed." });
            }

            // Match the database's UQ_Enrollments_UserEvent unique constraint.
            // A participant can only enroll once per event, regardless of category.
            var alreadyEnrolled = await _context.Enrollments
                .AnyAsync(e => e.UsersID == userId && e.EventsID == request.EventsID);

            if (alreadyEnrolled)
            {
                return Conflict(new { message = "You are already enrolled in this event." });
            }

            // Check the category's participant limit.
            var currentEnrollments = await _context.Enrollments
                .CountAsync(e =>
                    e.CategoriesID == request.CategoriesID &&
                    e.Status == "Confirmed");

            if (currentEnrollments >= category.MaxParticipants)
            {
                return Conflict(new { message = "This category has reached its participant limit." });
            }

            // Create the enrollment using the authenticated user's ID.
            var enrollment = new Enrollment
            {
                EnrolledAt = DateTime.Now,
                Status = "Confirmed",
                EmergencyContactName = request.EmergencyContactName,
                EmergencyContactPhone = request.EmergencyContactPhone,
                CategoriesID = request.CategoriesID,
                EventsID = request.EventsID,
                UsersID = userId
            };

            _context.Enrollments.Add(enrollment);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
                when (ex.InnerException is SqlException sqlEx &&
                      (sqlEx.Number == 2601 || sqlEx.Number == 2627) &&
                      sqlEx.Message.Contains("UQ_Enrollments_UserEvent"))
            {
                return Conflict(new { message = "You are already enrolled in this event." });
            }

            return CreatedAtAction(
                nameof(GetEnrollment),
                new { id = enrollment.EnrollmentsID },
                enrollment);
        }


        // DELETE: api/Enrollments/5
        // Participants can deregister only themselves.
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Participant")]
        public async Task<IActionResult> Deregister(int id)
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

            // Find only an enrollment belonging to this participant.
            var enrollment = await _context.Enrollments
                .FirstOrDefaultAsync(e =>
                    e.EnrollmentsID == id &&
                    e.UsersID == userId);

            if (enrollment == null)
            {
                return NotFound(new
                {
                    message = "Enrollment not found."
                });
            }

            // Do not delete an enrollment that has a race result.
            var hasResult = await _context.Results
                .AnyAsync(r => r.EnrollmentsID == id);

            if (hasResult)
            {
                return Conflict(new
                {
                    message = "You cannot deregister because a race result has already been recorded."
                });
            }

            // Delete the participant's enrollment.
            _context.Enrollments.Remove(enrollment);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return Conflict(new
                {
                    message = "This enrollment cannot be removed because it has related records."
                });
            }

            return NoContent();
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
