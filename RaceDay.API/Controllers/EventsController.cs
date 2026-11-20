using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.Models;
using RaceDay.API.Security;
using RaceDay.API.Services;

namespace RaceDayAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EventsController : ControllerBase
    {
        private readonly RaceDayDbContext _context;
        private readonly BlobService _blobService;

        public EventsController(RaceDayDbContext context, BlobService blobService)
        {
            _context = context;
            _blobService = blobService;
        }

        [HttpGet]
        [AuthorizeRole("Organiser", "Participant")]
        public async Task<IActionResult> GetAllEvents()
        {
            var events = await _context.Events
                .Include(e => e.Organiser)
                .Include(e => e.Categories)
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

        [HttpGet("{id}")]
        [AuthorizeRole("Organiser", "Participant")]
        public async Task<IActionResult> GetEvent(int id)
        {
            // Project to an anonymous type so entity cycles (Event -> Categories -> Event)
            // are never serialised and the organiser's password hash is never exposed.
            var ev = await _context.Events
                .Where(e => e.EventId == id)
                .Select(e => new
                {
                    e.EventId,
                    e.OrganiserId,
                    e.Name,
                    e.Description,
                    e.EventDate,
                    e.Location,
                    e.DistanceKm,
                    e.EventType,
                    e.BannerImageUrl,
                    OrganiserName = e.Organiser!.FirstName + " " + e.Organiser.LastName,
                    Categories = e.Categories.Select(c => new
                    {
                        c.CategoryId,
                        c.Name,
                        c.CategoryType,
                        c.MinAge,
                        c.MaxAge,
                        c.DistanceKm
                    }).ToList()
                })
                .FirstOrDefaultAsync();

            if (ev == null) return NotFound(new { message = "Event not found." });

            return Ok(ev);
        }

        [HttpGet("mine")]
        [AuthorizeRole("Organiser")]
        public async Task<IActionResult> GetMyEvents()
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (userIdStr == null) return Unauthorized();
            var organiserId = int.Parse(userIdStr);

            var myEvents = await _context.Events
                .Where(e => e.OrganiserId == organiserId)
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
                    CategoryCount = e.Categories.Count
                })
                .ToListAsync();

            return Ok(myEvents);
        }

        [HttpPost]
        [AuthorizeRole("Organiser")]
        public async Task<IActionResult> CreateEvent([FromForm] CreateEventDto dto, IFormFile? BannerImage)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (userIdStr == null) return Unauthorized();
            var organiserId = int.Parse(userIdStr);

            string? bannerUrl = null;

            // If an image was uploaded, validate it and send it to Azure Blob Storage
            if (BannerImage != null && BannerImage.Length > 0)
            {
                var imageError = ImageUploadRules.Validate(BannerImage);
                if (imageError != null) return BadRequest(new { message = imageError });

                bannerUrl = await _blobService.UploadFileAsync(BannerImage, "event-banners");
            }

            var newEvent = new Event
            {
                OrganiserId = organiserId,
                Name = dto.Name,
                Description = dto.Description,
                EventDate = dto.EventDate,
                Location = dto.Location,
                DistanceKm = dto.DistanceKm,
                EventType = dto.EventType,
                BannerImageUrl = bannerUrl,
                CreatedAt = DateTime.UtcNow
            };

            _context.Events.Add(newEvent);
            await _context.SaveChangesAsync();

            return StatusCode(201, new { message = "Event created successfully.", eventId = newEvent.EventId });
        }

        [HttpPut("{id}")]
        [AuthorizeRole("Organiser")]
        public async Task<IActionResult> UpdateEvent(int id, [FromForm] CreateEventDto dto, IFormFile? BannerImage)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (userIdStr == null) return Unauthorized();
            var organiserId = int.Parse(userIdStr);

            var existingEvent = await _context.Events.FindAsync(id);
            if (existingEvent == null) return NotFound(new { message = "Event not found." });

            if (existingEvent.OrganiserId != organiserId)
                return StatusCode(403, new { message = "You can only edit your own events." });

            existingEvent.Name = dto.Name;
            existingEvent.Description = dto.Description;
            existingEvent.EventDate = dto.EventDate;
            existingEvent.Location = dto.Location;
            existingEvent.DistanceKm = dto.DistanceKm;
            existingEvent.EventType = dto.EventType;

            // Only replace the banner when a new image is supplied; otherwise keep the existing one
            if (BannerImage != null && BannerImage.Length > 0)
            {
                var imageError = ImageUploadRules.Validate(BannerImage);
                if (imageError != null) return BadRequest(new { message = imageError });

                existingEvent.BannerImageUrl = await _blobService.UploadFileAsync(BannerImage, "event-banners");
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Event updated successfully." });
        }

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

            // Enrolments are protected by a Restrict foreign key, so refuse cleanly instead of failing with a 500
            var hasEnrolments = await _context.Enrolments.AnyAsync(e => e.EventId == id);
            if (hasEnrolments)
                return Conflict(new { message = "Cannot delete this event because participants are already enrolled." });

            _context.Events.Remove(existingEvent);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }

    public class CreateEventDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public string Location { get; set; } = string.Empty;
        public decimal DistanceKm { get; set; }
        public string EventType { get; set; } = string.Empty;
    }
}