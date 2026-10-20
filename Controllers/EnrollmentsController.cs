using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.Models;
using RaceDay.API.Security;

namespace RaceDay.API.Controllers
{
    [Route("api")]
    [ApiController]
    public class EnrolmentsController : ControllerBase
    {
        private readonly RaceDayDbContext _context;

        public EnrolmentsController(RaceDayDbContext context)
        {
            _context = context;
        }

        private int GetLoggedInUserId()
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            return string.IsNullOrEmpty(userIdStr) ? 0 : int.Parse(userIdStr);
        }

        // POST: api/events/{eventId}/enrolments
        // Participant enrolls in an event by selecting a category
        [HttpPost("events/{eventId}/enrolments")]
        [AuthorizeRole("Participant")]
        public async Task<IActionResult> EnrolInEvent(int eventId, [FromBody] CreateEnrolmentDto dto)
        {
            var participantId = GetLoggedInUserId();

            // 1. Check if the event exists
            var eventEntity = await _context.Events.FindAsync(eventId);
            if (eventEntity == null) return NotFound(new { message = "Event not found." });

            // 2. Check if the category exists AND belongs to this specific event
            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.CategoryId == dto.CategoryId && c.EventId == eventId);
            if (category == null)
                return BadRequest(new { message = "Invalid category. The category does not exist or does not belong to this event." });

            // 3. Check if the participant is already enrolled in this event
            var alreadyEnrolled = await _context.Enrolments
                .AnyAsync(e => e.EventId == eventId && e.ParticipantId == participantId);
            if (alreadyEnrolled)
                return Conflict(new { message = "You are already enrolled in this event." });

            // 4. Create the enrolment
            var newEnrolment = new Enrolment
            {
                EventId = eventId,
                CategoryId = dto.CategoryId,
                ParticipantId = participantId,
                EnrolmentDate = DateTime.UtcNow,
                Status = "Pending" // Default status as per Part 1 SQL plan
            };

            _context.Enrolments.Add(newEnrolment);
            await _context.SaveChangesAsync();

            return StatusCode(201, new { message = "Successfully enrolled in the event.", enrolmentId = newEnrolment.EnrolmentId });
        }

        // GET: api/enrolments/mine
        // Participant views all their own enrolments
        [HttpGet("enrolments/mine")]
        [AuthorizeRole("Participant")]
        public async Task<IActionResult> GetMyEnrolments()
        {
            var participantId = GetLoggedInUserId();

            var myEnrolments = await _context.Enrolments
                .Include(e => e.Event)
                .Include(e => e.Category)
                .Where(e => e.ParticipantId == participantId)
                .Select(e => new
                {
                    e.EnrolmentId,
                    EventName = e.Event!.Name,
                    EventDate = e.Event.EventDate,
                    CategoryName = e.Category!.Name,
                    e.EnrolmentDate,
                    e.Status
                })
                .ToListAsync();

            return Ok(myEnrolments);
        }

        // GET: api/events/{eventId}/enrolments
        // Organiser views all enrolments for their specific event
        [HttpGet("events/{eventId}/enrolments")]
        [AuthorizeRole("Organiser")]
        public async Task<IActionResult> GetEventEnrolments(int eventId)
        {
            var organiserId = GetLoggedInUserId();

            // Verify the event belongs to the logged-in organiser
            var eventEntity = await _context.Events.FindAsync(eventId);
            if (eventEntity == null) return NotFound(new { message = "Event not found." });
            if (eventEntity.OrganiserId != organiserId)
                return StatusCode(403, new { message = "You can only view enrolments for your own events." });

            var enrolments = await _context.Enrolments
                .Include(e => e.Participant)
                .Include(e => e.Category)
                .Where(e => e.EventId == eventId)
                .Select(e => new
                {
                    e.EnrolmentId,
                    ParticipantName = $"{e.Participant!.FirstName} {e.Participant.LastName}",
                    ParticipantEmail = e.Participant.Email,
                    CategoryName = e.Category!.Name,
                    e.EnrolmentDate,
                    e.Status
                })
                .ToListAsync();

            return Ok(enrolments);
        }

        // PUT: api/enrolments/{id}/status
        // Organiser confirms or cancels a participant's enrolment
        [HttpPut("enrolments/{id}/status")]
        [AuthorizeRole("Organiser")]
        public async Task<IActionResult> UpdateEnrolmentStatus(int id, [FromBody] UpdateEnrolmentStatusDto dto)
        {
            var organiserId = GetLoggedInUserId();

            // Valid statuses: "Pending", "Confirmed", "Cancelled"
            var validStatuses = new[] { "Pending", "Confirmed", "Cancelled" };
            if (!validStatuses.Contains(dto.Status))
                return BadRequest(new { message = "Invalid status. Choose 'Pending', 'Confirmed', or 'Cancelled'." });

            var enrolment = await _context.Enrolments
                .Include(e => e.Event)
                .FirstOrDefaultAsync(e => e.EnrolmentId == id);

            if (enrolment == null) return NotFound(new { message = "Enrolment not found." });

            // Verify the event belongs to the logged-in organiser
            if (enrolment.Event!.OrganiserId != organiserId)
                return StatusCode(403, new { message = "You can only manage enrolments for your own events." });

            enrolment.Status = dto.Status;
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Enrolment status updated to {dto.Status}." });
        }

        // DELETE: api/enrolments/{id}
        // Participant withdraws their own enrolment (Sets status to 'Cancelled')
        [HttpDelete("enrolments/{id}")]
        [AuthorizeRole("Participant")]
        public async Task<IActionResult> WithdrawEnrolment(int id)
        {
            var participantId = GetLoggedInUserId();

            var enrolment = await _context.Enrolments.FindAsync(id);
            if (enrolment == null) return NotFound(new { message = "Enrolment not found." });

            // Ensure the participant is withdrawing their OWN enrolment
            if (enrolment.ParticipantId != participantId)
                return StatusCode(403, new { message = "You can only withdraw your own enrolments." });

            // Instead of deleting the record, we set it to Cancelled to keep the history
            enrolment.Status = "Cancelled";
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }

    // DTOs for Enrolments
    public class CreateEnrolmentDto
    {
        public int CategoryId { get; set; }
    }

    public class UpdateEnrolmentStatusDto
    {
        public string Status { get; set; } = string.Empty;
    }
}