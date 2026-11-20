using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.Models;
using RaceDay.API.Security;
using RaceDay.API.Services;

namespace RaceDay.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly RaceDayDbContext _context;
        private readonly BlobService _blobService;

        public UsersController(RaceDayDbContext context, BlobService blobService)
        {
            _context = context;
            _blobService = blobService;
        }

        private int GetLoggedInUserId()
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            return string.IsNullOrEmpty(userIdStr) ? 0 : int.Parse(userIdStr);
        }

        // GET: api/users/profile
        [HttpGet("profile")]
        [AuthorizeRole("Organiser", "Participant")]
        public async Task<IActionResult> GetProfile()
        {
            var userId = GetLoggedInUserId();
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null) return NotFound();

            return Ok(new
            {
                user.UserId,
                user.FirstName,
                user.LastName,
                user.Email,
                user.PhoneNumber,
                user.DateOfBirth,
                user.ProfilePictureUrl,
                Role = user.Role!.RoleName
            });
        }

        // POST: api/users/profile-picture
        [HttpPost("profile-picture")]
        [AuthorizeRole("Organiser", "Participant")]
        public async Task<IActionResult> UploadProfilePicture(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "No file provided." });

            var userId = GetLoggedInUserId();
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            // Upload to a specific container for profile pictures
            var imageUrl = await _blobService.UploadFileAsync(file, "profile-pictures");

            user.ProfilePictureUrl = imageUrl;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Profile picture updated.", imageUrl = user.ProfilePictureUrl });
        }
    }
}