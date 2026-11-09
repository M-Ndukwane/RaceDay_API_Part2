using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.Models;
using Microsoft.AspNetCore.Authorization;

namespace RaceDay.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VenuesController : ControllerBase
    {
        private readonly RaceDayDbContext _context;

        public VenuesController(RaceDayDbContext context)
        {
            _context = context;
        }

        // GET: api/Venues
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Venue>>> GetVenues()
        {
            var venues = await _context.Venues
                .AsNoTracking()
                .ToListAsync();

            return Ok(venues);
        }

        // GET: api/Venues/1
        [HttpGet("{id:int}")]
        public async Task<ActionResult<Venue>> GetVenue(int id)
        {
            var venue = await _context.Venues
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.VenuesID == id);

            if (venue == null)
            {
                return NotFound(new
                {
                    message = "Venue not found."
                });
            }

            return Ok(venue);
        }
        // Create a new venue
        [HttpPost]
        [Authorize(Roles = "Organiser")]
        public async Task<ActionResult<Venue>> CreateVenue(Venue venue)
        {
            venue.VenuesID = 0;

            _context.Venues.Add(venue);

            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetVenue),
                new { id = venue.VenuesID },
                venue
            );
        }
        // Update an existing venue
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> UpdateVenue(int id, Venue updatedVenue)
        {
            var venue = await _context.Venues
                .FirstOrDefaultAsync(v => v.VenuesID == id);

            if (venue == null)
            {
                return NotFound(new
                {
                    message = "Venue not found."
                });
            }

            venue.VenueName = updatedVenue.VenueName;
            venue.AddressLine = updatedVenue.AddressLine;
            venue.City = updatedVenue.City;
            venue.PostalCode = updatedVenue.PostalCode;
            venue.Province = updatedVenue.Province;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Venue updated successfully.",
                venue
            });
        }
        // Delete an existing venue
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> DeleteVenue(int id)
        {
            var venue = await _context.Venues
                .FirstOrDefaultAsync(v => v.VenuesID == id);

            if (venue == null)
            {
                return NotFound(new
                {
                    message = "Venue not found."
                });
            }

            // Check whether the venue is being used by an event
            var hasEvents = await _context.Events
                .AnyAsync(e => e.VenuesID == id);

            if (hasEvents)
            {
                return Conflict(new
                {
                    message = "This venue cannot be deleted because it is linked to an event."
                });
            }

            _context.Venues.Remove(venue);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Venue deleted successfully."
            });
        }
    }
}
