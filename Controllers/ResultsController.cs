using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.Models;
using RaceDay.API.Security;

namespace RaceDay.API.Controllers
{
    [Route("api")]
    [ApiController]
    public class ResultsController : ControllerBase
    {
        private readonly RaceDayDbContext _context;

        public ResultsController(RaceDayDbContext context)
        {
            _context = context;
        }

        private int GetLoggedInUserId()
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            return string.IsNullOrEmpty(userIdStr) ? 0 : int.Parse(userIdStr);
        }

        // POST: api/events/{eventId}/results
        // Organiser captures the finish time and position for an enrolled participant
        [HttpPost("events/{eventId}/results")]
        [AuthorizeRole("Organiser")]
        public async Task<IActionResult> CaptureResult(int eventId, [FromBody] CreateResultDto dto)
        {
            var organiserId = GetLoggedInUserId();

            // 1. Verify event exists and belongs to organiser
            var eventEntity = await _context.Events.FindAsync(eventId);
            if (eventEntity == null) return NotFound(new { message = "Event not found." });
            if (eventEntity.OrganiserId != organiserId)
                return StatusCode(403, new { message = "You can only manage results for your own events." });

            // 2. Find the enrolment
            var enrolment = await _context.Enrolments
                .FirstOrDefaultAsync(e => e.EnrolmentId == dto.EnrolmentId && e.EventId == eventId);
            if (enrolment == null)
                return NotFound(new { message = "Enrolment not found for this event." });

            // 3. Check if a result already exists
            var existingResult = await _context.Results.AnyAsync(r => r.EnrolmentId == dto.EnrolmentId);
            if (existingResult)
                return Conflict(new { message = "A result has already been captured for this participant." });

            // 4. Create the result
            var newResult = new Result
            {
                EnrolmentId = dto.EnrolmentId,
                FinishTime = dto.FinishTime,
                FinishPosition = dto.FinishPosition,
                RecordedAt = DateTime.UtcNow
            };

            _context.Results.Add(newResult);

            // Optionally, you might want to auto-confirm the enrolment when a result is added
            enrolment.Status = "Confirmed";

            await _context.SaveChangesAsync();

            return StatusCode(201, new { message = "Result captured successfully.", resultId = newResult.ResultId });
        }

        // PUT: api/results/{id}
        // Organiser corrects an existing result
        [HttpPut("results/{id}")]
        [AuthorizeRole("Organiser")]
        public async Task<IActionResult> UpdateResult(int id, [FromBody] UpdateResultDto dto)
        {
            var organiserId = GetLoggedInUserId();

            var result = await _context.Results
                .Include(r => r.Enrolment)
                    .ThenInclude(e => e!.Event)
                .FirstOrDefaultAsync(r => r.ResultId == id);

            if (result == null) return NotFound(new { message = "Result not found." });

            if (result.Enrolment!.Event!.OrganiserId != organiserId)
                return StatusCode(403, new { message = "You can only manage results for your own events." });

            result.FinishTime = dto.FinishTime;
            result.FinishPosition = dto.FinishPosition;
            result.RecordedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Result updated successfully." });
        }

        // GET: api/events/{eventId}/results
        // Organiser views all published results for their event
        [HttpGet("events/{eventId}/results")]
        [AuthorizeRole("Organiser")]
        public async Task<IActionResult> GetEventResults(int eventId)
        {
            var organiserId = GetLoggedInUserId();

            var eventEntity = await _context.Events.FindAsync(eventId);
            if (eventEntity == null) return NotFound(new { message = "Event not found." });
            if (eventEntity.OrganiserId != organiserId)
                return StatusCode(403, new { message = "You can only view results for your own events." });

            var results = await _context.Results
                .Include(r => r.Enrolment)
                    .ThenInclude(e => e!.Participant)
                .Where(r => r.Enrolment!.EventId == eventId)
                .OrderBy(r => r.FinishPosition)
                .Select(r => new
                {
                    r.ResultId,
                    ParticipantName = $"{r.Enrolment!.Participant!.FirstName} {r.Enrolment.Participant.LastName}",
                    r.FinishTime,
                    r.FinishPosition
                })
                .ToListAsync();

            return Ok(results);
        }

        // GET: api/results/mine
        // Participant views their personal race history
        [HttpGet("results/mine")]
        [AuthorizeRole("Participant")]
        public async Task<IActionResult> GetMyResults()
        {
            var participantId = GetLoggedInUserId();

            // We use a projection to calculate the total number of finishers for each event
            var myResults = await _context.Results
                .Include(r => r.Enrolment)
                    .ThenInclude(e => e!.Event)
                .Where(r => r.Enrolment!.ParticipantId == participantId)
                .Select(r => new
                {
                    r.ResultId,
                    EventName = r.Enrolment!.Event!.Name,
                    EventDate = r.Enrolment.Event.EventDate,
                    r.FinishTime,
                    r.FinishPosition,
                    // Count how many results exist for this specific event
                    TotalFinishers = _context.Results.Count(res => res.Enrolment!.EventId == r.Enrolment.EventId)
                })
                .ToListAsync();

            return Ok(myResults);
        }
    }

    // DTOs for Results
    public class CreateResultDto
    {
        public int EnrolmentId { get; set; }
        public TimeSpan FinishTime { get; set; }
        public int FinishPosition { get; set; }
    }

    public class UpdateResultDto
    {
        public TimeSpan FinishTime { get; set; }
        public int FinishPosition { get; set; }
    }
}