using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.Models;
using RaceDay.API.Security;
using RaceDay.API.Security;
using System.Security.Claims;

namespace RaceDayAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EventsController : ControllerBase
    {
        private readonly RaceDayDbContext _context;

        public EventsController(RaceDayDbContext context)
        {
            _context = context;
        }

        // GET: api/events
        // Any logged-in user (Organiser or Participant) can view all events
        [HttpGet]
        [AuthorizeRole("Organiser", "Participant")]
        public async Task<IActionResult> GetAllEvents()
        {
            var events = await _context.Events
                .Include(e => e.Organiser) // Include organiser details
                .Include(e => e.Categories) // Include categories
                .Select(e => new
                {
                    e.EventId,
                    e.Name,
                    e.Description,
                    e.EventDate,
                    e.Location,
                    e.DistanceKm,
                    e.EventType,
                    e.BannerImageUrl,
                    OrganiserName = $"{e.Organiser!.FirstName} {e.Organiser.LastName}",
                    CategoryCount = e.Categories.Count
                })
                .ToListAsync();

            return Ok(events);
        }

        // GET: api/events/{id}
        // Any logged-in user can view a specific event
        [HttpGet("{id}")]
        [AuthorizeRole("Organiser", "Participant")]
        public async Task<IActionResult> GetEvent(int id)
        {
            var ev = await _context.Events
                .Include(e => e.Organiser)
                .Include(e => e.Categories)
                .FirstOrDefaultAsync(e => e.EventId == id);

            if (ev == null) return NotFound(new { message = "Event not found." });

            return Ok(ev);
        }

        // GET: api/events/mine
        // Only Organisers can see the events they created
        [HttpGet("mine")]
        [AuthorizeRole("Organiser")]
        public async Task<IActionResult> GetMyEvents()
        {
            // Get the logged-in user's ID from the Session
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (userIdStr == null) return Unauthorized();
            var organiserId = int.Parse(userIdStr);

            var myEvents = await _context.Events
                .Where(e => e.OrganiserId == organiserId)
                .Include(e => e.Categories)
                .ToListAsync();

            return Ok(myEvents);
        }

        // POST: api/events
        // Only Organisers can create events
        [HttpPost]
        [AuthorizeRole("Organiser")]
        public async Task<IActionResult> CreateEvent([FromBody] CreateEventDto dto)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (userIdStr == null) return Unauthorized();
            var organiserId = int.Parse(userIdStr);

            var newEvent = new Event
            {
                OrganiserId = organiserId,
                Name = dto.Name,
                Description = dto.Description,
                EventDate = dto.EventDate,
                Location = dto.Location,
                DistanceKm = dto.DistanceKm,
                EventType = dto.EventType,
                CreatedAt = DateTime.UtcNow
            };

            _context.Events.Add(newEvent);
            await _context.SaveChangesAsync();

            return StatusCode(201, new { message = "Event created successfully.", eventId = newEvent.EventId });
        }

        // PUT: api/events/{id}
        // Only the Organiser who created the event can edit it
        [HttpPut("{id}")]
        [AuthorizeRole("Organiser")]
        public async Task<IActionResult> UpdateEvent(int id, [FromBody] CreateEventDto dto)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (userIdStr == null) return Unauthorized();
            var organiserId = int.Parse(userIdStr);

            var existingEvent = await _context.Events.FindAsync(id);
            if (existingEvent == null) return NotFound(new { message = "Event not found." });

            // Check if the logged-in organiser is the one who created this event
            if (existingEvent.OrganiserId != organiserId)
                return StatusCode(403, new { message = "You can only edit your own events." });

            existingEvent.Name = dto.Name;
            existingEvent.Description = dto.Description;
            existingEvent.EventDate = dto.EventDate;
            existingEvent.Location = dto.Location;
            existingEvent.DistanceKm = dto.DistanceKm;
            existingEvent.EventType = dto.EventType;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Event updated successfully." });
        }

        // DELETE: api/events/{id}
        // Only the Organiser who created it can delete it
        [HttpDelete("{id}")]
        [AuthorizeRole("Organiser")]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (userIdStr == null) return Unauthorized();
            var organiserId = int.Parse(userIdStr);

            var existingEvent = await _context.Events.FindAsync(id);
            if (existingEvent == null) return NotFound(new { message = "Event not found." });

            if (existingEvent.OrganiserId != organiserId)
                return StatusCode(403, new { message = "You can only delete your own events." });

            _context.Events.Remove(existingEvent);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }

    // DTO to keep the API clean and prevent over-posting
    public class CreateEventDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public string Location { get; set; } = string.Empty;
        public decimal DistanceKm { get; set; }
        public string EventType { get; set; } = string.Empty; // "Run", "Walk", or "Cycle"
    }
}