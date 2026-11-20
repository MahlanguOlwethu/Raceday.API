using Microsoft.AspNetCore.Http;

namespace RaceDay.MVC.ViewModels
{
    public class EventViewModel
    {
        public int EventId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public string Location { get; set; } = string.Empty;
        public decimal DistanceKm { get; set; }
        public string EventType { get; set; } = string.Empty;
        public string? BannerImageUrl { get; set; }
        public string OrganiserName { get; set; } = string.Empty;
        public int CategoryCount { get; set; }
    }

    public class EventDetailsViewModel
    {
        public int EventId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public string Location { get; set; } = string.Empty;
        public decimal DistanceKm { get; set; }
        public string EventType { get; set; } = string.Empty;
        public string? BannerImageUrl { get; set; }
        public List<CategoryViewModel> Categories { get; set; } = new();
    }

    public class CategoryViewModel
    {
        public int CategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string CategoryType { get; set; } = string.Empty;
        public int? MinAge { get; set; }
        public int? MaxAge { get; set; }
        public decimal? DistanceKm { get; set; }
    }

    public class CreateEventViewModel
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime EventDate { get; set; } = DateTime.Now.AddDays(7);
        public string Location { get; set; } = string.Empty;
        public decimal DistanceKm { get; set; }
        public string EventType { get; set; } = "Run"; // Run, Walk, Cycle
        public IFormFile? BannerImage { get; set; }
    }

    public class EditEventViewModel : CreateEventViewModel
    {
        public int EventId { get; set; }
        public string? ExistingBannerUrl { get; set; }
    }

    public class CreateCategoryViewModel
    {
        public string Name { get; set; } = string.Empty;
        public string CategoryType { get; set; } = "Distance"; // Distance or Age
        public int? MinAge { get; set; }
        public int? MaxAge { get; set; }
        public decimal? DistanceKm { get; set; }
    }

    public class ParticipantResultViewModel
    {
        public int EnrolmentId { get; set; }
        public string ParticipantName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public class ProfileViewModel
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public IFormFile? ProfilePictureFile { get; set; }
    }
}