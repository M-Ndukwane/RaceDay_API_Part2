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
    public class ResultsController : ControllerBase
    {
        private readonly RaceDayDbContext _context;

        public ResultsController(RaceDayDbContext context)
        {
            _context = context;
        }

        // GET: api/Results
        // Any authenticated user can view results.
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Result>>> GetResults()
        {
            var results = await _context.Results
                .AsNoTracking()
                .ToListAsync();

            return Ok(results);
        }

        // GET: api/Results/5
        // Any authenticated user can view a specific result.
        [HttpGet("{id:int}")]
        public async Task<ActionResult<Result>> GetResult(int id)
        {
            var result = await _context.Results
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.ResultsID == id);

            if (result == null)
            {
                return NotFound(new
                {
                    message = "Result not found."
                });
            }

            return Ok(result);
        }

        // POST: api/Results
        // Only organisers can record results.
        [HttpPost]
        [Authorize(Roles = "Organiser")]
        public async Task<ActionResult<Result>> CreateResult(
            CreateResultRequest request)
        {
            if (request.Position.HasValue &&
                request.Position.Value <= 0)
            {
                return BadRequest(new
                {
                    message = "Position must be greater than zero."
                });
            }

            if (request.FinishTimeSeconds.HasValue &&
                request.FinishTimeSeconds.Value < 0)
            {
                return BadRequest(new
                {
                    message = "Finish time cannot be negative."
                });
            }

            if (string.IsNullOrWhiteSpace(request.ResultStatus))
            {
                return BadRequest(new
                {
                    message = "Result status is required."
                });
            }

            var validStatuses = new[]
            {
                "Finished",
                "DidNotFinish",
                "Disqualified"
            };

            if (!validStatuses.Contains(
                request.ResultStatus,
                StringComparer.OrdinalIgnoreCase))
            {
                return BadRequest(new
                {
                    message = "Invalid result status."
                });
            }

            var enrollment = await _context.Enrollments
                .AsNoTracking()
                .FirstOrDefaultAsync(e =>
                    e.EnrollmentsID == request.EnrollmentsID);

            if (enrollment == null)
            {
                return NotFound(new
                {
                    message = "Enrollment not found."
                });
            }

            var existingResult = await _context.Results
                .AnyAsync(r =>
                    r.EnrollmentsID == request.EnrollmentsID);

            if (existingResult)
            {
                return Conflict(new
                {
                    message = "A result already exists for this enrollment."
                });
            }

            var result = new Result
            {
                Position = request.Position,
                FinishTimeSeconds = request.FinishTimeSeconds,
                ResultStatus = request.ResultStatus,
                RecordedAt = DateTime.UtcNow,
                EnrollmentsID = request.EnrollmentsID
            };

            _context.Results.Add(result);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetResult),
                new { id = result.ResultsID },
                result);
        }

        // PUT: api/Results/5
        // Only organisers can update results.
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> UpdateResult(
            int id,
            UpdateResultRequest request)
        {
            var result = await _context.Results
                .FirstOrDefaultAsync(r => r.ResultsID == id);

            if (result == null)
            {
                return NotFound(new
                {
                    message = "Result not found."
                });
            }

            if (request.Position.HasValue &&
                request.Position.Value <= 0)
            {
                return BadRequest(new
                {
                    message = "Position must be greater than zero."
                });
            }

            if (request.FinishTimeSeconds.HasValue &&
                request.FinishTimeSeconds.Value < 0)
            {
                return BadRequest(new
                {
                    message = "Finish time cannot be negative."
                });
            }

            if (string.IsNullOrWhiteSpace(request.ResultStatus))
            {
                return BadRequest(new
                {
                    message = "Result status is required."
                });
            }

            var validStatuses = new[]
            {
                "Finished",
                "DidNotFinish",
                "Disqualified"
            };

            if (!validStatuses.Contains(
                request.ResultStatus,
                StringComparer.OrdinalIgnoreCase))
            {
                return BadRequest(new
                {
                    message = "Invalid result status."
                });
            }

            result.Position = request.Position;
            result.FinishTimeSeconds = request.FinishTimeSeconds;
            result.ResultStatus = request.ResultStatus;
            result.RecordedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/Results/5
        // Only organisers can delete results.
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> DeleteResult(int id)
        {
            var result = await _context.Results
                .FirstOrDefaultAsync(r => r.ResultsID == id);

            if (result == null)
            {
                return NotFound(new
                {
                    message = "Result not found."
                });
            }

            _context.Results.Remove(result);

            await _context.SaveChangesAsync();

            return NoContent();
        }
    }

    // Request body for POST: api/Results
    public class CreateResultRequest
    {
        public int? Position { get; set; }

        public int? FinishTimeSeconds { get; set; }

        public string ResultStatus { get; set; } = "Finished";

        public int EnrollmentsID { get; set; }
    }

    // Request body for PUT: api/Results/{id}
    public class UpdateResultRequest
    {
        public int? Position { get; set; }

        public int? FinishTimeSeconds { get; set; }

        public string ResultStatus { get; set; } = "Finished";
    }
}