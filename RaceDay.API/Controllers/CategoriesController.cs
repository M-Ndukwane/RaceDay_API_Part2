
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoriesController : ControllerBase
    {
        private readonly RaceDayDbContext _context;

        public CategoriesController(RaceDayDbContext context)
        {
            _context = context;
        }

        // GET: api/Categories
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Category>>> GetCategories()
        {
            var categories = await _context.Categories
                .AsNoTracking()
                .ToListAsync();

            return Ok(categories);
        }

        // GET: api/Categories/5
        [HttpGet("{id:int}")]
        public async Task<ActionResult<Category>> GetCategory(int id)
        {
            var category = await _context.Categories
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CategoriesID == id);

            if (category == null)
                return NotFound(new { message = "Category not found." });

            return Ok(category);
        }

        // POST: api/Categories
        [Authorize(Roles = "Organiser")]
        [HttpPost]
        public async Task<ActionResult> CreateCategory(
            CategoryRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.CategoryName))
                return BadRequest(new { message = "Category name is required." });

            if (request.EntryFee < 0)
                return BadRequest(new { message = "Entry fee cannot be negative." });

            if (request.MaxParticipants <= 0)
                return BadRequest(new { message = "Maximum participants must be greater than zero." });

            var eventExists = await _context.Events
                .AnyAsync(e => e.EventsID == request.EventsID);

            if (!eventExists)
                return BadRequest(new { message = "The selected event does not exist." });

            var category = new Category
            {
                CategoryName = request.CategoryName.Trim(),
                EntryFee = request.EntryFee,
                MaxParticipants = request.MaxParticipants,
                EventsID = request.EventsID
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetCategory),
                new { id = category.CategoriesID },
                category);
        }

        // PUT: api/Categories/5
        [Authorize(Roles = "Organiser")]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateCategory(
            int id, CategoryRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.CategoryName))
                return BadRequest(new { message = "Category name is required." });

            if (request.EntryFee < 0)
                return BadRequest(new { message = "Entry fee cannot be negative." });

            if (request.MaxParticipants <= 0)
                return BadRequest(new { message = "Maximum participants must be greater than zero." });

            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.CategoriesID == id);

            if (category == null)
                return NotFound(new { message = "Category not found." });

            var eventExists = await _context.Events
                .AnyAsync(e => e.EventsID == request.EventsID);

            if (!eventExists)
                return BadRequest(new { message = "The selected event does not exist." });

            category.CategoryName = request.CategoryName.Trim();
            category.EntryFee = request.EntryFee;
            category.MaxParticipants = request.MaxParticipants;
            category.EventsID = request.EventsID;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/Categories/5
        [Authorize(Roles = "Organiser")]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.CategoriesID == id);

            if (category == null)
                return NotFound(new { message = "Category not found." });

            _context.Categories.Remove(category);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return Conflict(new
                {
                    message = "This category has related enrollments and cannot be deleted until those relationships are resolved."
                });
            }

            return NoContent();
        }
    }

    public class CategoryRequest
    {
        public string CategoryName { get; set; } = string.Empty;
        public decimal EntryFee { get; set; }
        public int MaxParticipants { get; set; }
        public int EventsID { get; set; }
    }
}

