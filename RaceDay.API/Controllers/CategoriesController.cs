using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.Models;
using RaceDay.API.Security;

namespace RaceDay.API.Controllers
{
    [Route("api")]
    [ApiController]
    public class CategoriesController : ControllerBase
    {
        private readonly RaceDayDbContext _context;

        public CategoriesController(RaceDayDbContext context)
        {
            _context = context;
        }

        // Helper method to get the logged-in user's ID
        private int GetLoggedInUserId()
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            return string.IsNullOrEmpty(userIdStr) ? 0 : int.Parse(userIdStr);
        }

        // GET: api/events/{eventId}/categories
        // Any logged-in user can view categories for an event
        [HttpGet("events/{eventId}/categories")]
        [AuthorizeRole("Organiser", "Participant")]
        public async Task<IActionResult> GetCategoriesForEvent(int eventId)
        {
            var eventExists = await _context.Events.AnyAsync(e => e.EventId == eventId);
            if (!eventExists) return NotFound(new { message = "Event not found." });

            var categories = await _context.Categories
                .Where(c => c.EventId == eventId)
                .ToListAsync();

            return Ok(categories);
        }

        // POST: api/events/{eventId}/categories
        // Only the Organiser who created the event can add categories
        [HttpPost("events/{eventId}/categories")]
        [AuthorizeRole("Organiser")]
        public async Task<IActionResult> CreateCategory(int eventId, [FromBody] CreateCategoryDto dto)
        {
            var organiserId = GetLoggedInUserId();
            var eventEntity = await _context.Events.FindAsync(eventId);

            if (eventEntity == null) return NotFound(new { message = "Event not found." });
            if (eventEntity.OrganiserId != organiserId)
                return StatusCode(403, new { message = "You can only add categories to your own events." });

            // Check for duplicate category name for this event
            var duplicate = await _context.Categories
                .AnyAsync(c => c.EventId == eventId && c.Name.ToLower() == dto.Name.ToLower());
            if (duplicate) return Conflict(new { message = "A category with this name already exists for this event." });

            var newCategory = new Category
            {
                EventId = eventId,
                Name = dto.Name,
                CategoryType = dto.CategoryType,
                MinAge = dto.MinAge,
                MaxAge = dto.MaxAge,
                DistanceKm = dto.DistanceKm
            };

            _context.Categories.Add(newCategory);
            await _context.SaveChangesAsync();

            return StatusCode(201, new { message = "Category created successfully.", categoryId = newCategory.CategoryId });
        }

        // PUT: api/categories/{id}
        // Only the owning Organiser can update a category
        [HttpPut("categories/{id}")]
        [AuthorizeRole("Organiser")]
        public async Task<IActionResult> UpdateCategory(int id, [FromBody] CreateCategoryDto dto)
        {
            var organiserId = GetLoggedInUserId();
            var category = await _context.Categories
                .Include(c => c.Event)
                .FirstOrDefaultAsync(c => c.CategoryId == id);

            if (category == null) return NotFound(new { message = "Category not found." });
            if (category.Event!.OrganiserId != organiserId)
                return StatusCode(403, new { message = "You can only edit categories for your own events." });

            category.Name = dto.Name;
            category.CategoryType = dto.CategoryType;
            category.MinAge = dto.MinAge;
            category.MaxAge = dto.MaxAge;
            category.DistanceKm = dto.DistanceKm;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Category updated successfully." });
        }

        // DELETE: api/categories/{id}
        // Only the owning Organiser can delete a category
        [HttpDelete("categories/{id}")]
        [AuthorizeRole("Organiser")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var organiserId = GetLoggedInUserId();
            var category = await _context.Categories
                .Include(c => c.Event)
                .Include(c => c.Enrolments)
                .FirstOrDefaultAsync(c => c.CategoryId == id);

            if (category == null) return NotFound(new { message = "Category not found." });
            if (category.Event!.OrganiserId != organiserId)
                return StatusCode(403, new { message = "You can only delete categories for your own events." });

            // Prevent deletion if participants are already enrolled in this category
            if (category.Enrolments.Any())
                return Conflict(new { message = "Cannot delete category because participants are already enrolled in it." });

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }

    // DTO for creating/updating categories
    public class CreateCategoryDto
    {
        public string Name { get; set; } = string.Empty;
        public string CategoryType { get; set; } = string.Empty; // "Age" or "Distance"
        public int? MinAge { get; set; }
        public int? MaxAge { get; set; }
        public decimal? DistanceKm { get; set; }
    }
}