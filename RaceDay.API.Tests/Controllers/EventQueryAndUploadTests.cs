using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using RaceDay.API.Models;
using RaceDay.API.Services;
using RaceDayAPI.Controllers;
using System.Collections;
using Xunit;

namespace RaceDay.API.Tests.Controllers
{
    // Covers the Part 3 additions: event filtering for the participant home page,
    // enrolment counts for the organiser dashboard, and safe handling of uploads
    // when Azure Blob Storage is not configured.
    public class EventQueryAndUploadTests
    {
        private static EventsController CreateController(RaceDay.API.Data.RaceDayDbContext context, string userId, string role)
        {
            var blob = new BlobService(new ConfigurationBuilder().AddInMemoryCollection().Build());
            var controller = new EventsController(context, blob);
            SessionMockHelper.SetSessionData(controller, userId, role);
            return controller;
        }

        private static RaceDay.API.Data.RaceDayDbContext SeedExtraEvents()
        {
            var context = TestHelpers.CreateSeededContext();
            context.Events.AddRange(
                new Event { EventId = 2, OrganiserId = 1, Name = "Cape Cycle Classic", Description = "d", EventDate = DateTime.Today.AddDays(40), Location = "Cape Town", DistanceKm = 109m, EventType = "Cycle" },
                new Event { EventId = 3, OrganiserId = 3, Name = "Old Charity Walk", Description = "d", EventDate = DateTime.Today.AddDays(-10), Location = "Durban", DistanceKm = 5m, EventType = "Walk" });
            context.Users.Add(new User { UserId = 4, RoleId = 2, FirstName = "Sam", LastName = "Runner", Email = "sam@test.com", PasswordHash = "x" });
            context.Enrolments.AddRange(
                new Enrolment { EventId = 1, CategoryId = 1, ParticipantId = 2, Status = "Confirmed", EnrolmentDate = DateTime.UtcNow },
                new Enrolment { EventId = 1, CategoryId = 1, ParticipantId = 4, Status = "Cancelled", EnrolmentDate = DateTime.UtcNow });
            context.SaveChanges();
            return context;
        }

        private static List<object> Items(IActionResult result) =>
            ((IEnumerable)Assert.IsType<OkObjectResult>(result).Value!).Cast<object>().ToList();

        [Fact]
        public async Task GetAllEvents_FilterByType_ReturnsOnlyThatType()
        {
            var controller = CreateController(SeedExtraEvents(), "2", "Participant");

            var items = Items(await controller.GetAllEvents(type: "Cycle"));

            Assert.Single(items);
            Assert.Equal("Cape Cycle Classic", TestHelpers.Prop(items[0], "Name"));
        }

        [Fact]
        public async Task GetAllEvents_UpcomingOnly_ExcludesPastEvents()
        {
            var controller = CreateController(SeedExtraEvents(), "2", "Participant");

            var items = Items(await controller.GetAllEvents(upcomingOnly: true));

            Assert.Equal(2, items.Count);
            Assert.DoesNotContain(items, i => (string?)TestHelpers.Prop(i, "Name") == "Old Charity Walk");
        }

        [Fact]
        public async Task GetAllEvents_SearchMatchesLocation()
        {
            var controller = CreateController(SeedExtraEvents(), "2", "Participant");

            var items = Items(await controller.GetAllEvents(search: "durban"));

            Assert.Single(items);
        }

        [Fact]
        public async Task GetMyEvents_ReturnsEnrolmentCountsExcludingCancelled()
        {
            var controller = CreateController(SeedExtraEvents(), "1", "Organiser");

            var items = Items(await controller.GetMyEvents());
            var parkRun = items.Single(i => (int)TestHelpers.Prop(i, "EventId")! == 1);

            Assert.Equal(2, items.Count); // organiser 1 owns events 1 and 2 only
            Assert.Equal(1, TestHelpers.Prop(parkRun, "EnrolmentCount"));
            Assert.Equal(1, TestHelpers.Prop(parkRun, "ConfirmedCount"));
            Assert.Equal(0, TestHelpers.Prop(parkRun, "PendingCount"));
        }

        [Fact]
        public async Task CreateEvent_WithBannerButNoAzureConfig_Returns503AndSavesNothing()
        {
            var context = TestHelpers.CreateSeededContext();
            var controller = CreateController(context, "1", "Organiser");
            var bytes = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
            IFormFile banner = new FormFile(new MemoryStream(bytes), 0, bytes.Length, "BannerImage", "banner.png")
            {
                Headers = new HeaderDictionary(),
                ContentType = "image/png"
            };
            var dto = new CreateEventDto { Name = "New", Description = "d", EventDate = DateTime.Today.AddDays(5), Location = "Soweto", DistanceKm = 21m, EventType = "Run" };

            var result = await controller.CreateEvent(dto, banner);

            Assert.Equal(503, Assert.IsAssignableFrom<ObjectResult>(result).StatusCode);
            Assert.Single(context.Events); // only the seeded event
        }

        [Fact]
        public void BlobService_WithoutConnectionString_CanStillBeConstructed()
        {
            var blob = new BlobService(new ConfigurationBuilder().AddInMemoryCollection().Build());

            Assert.False(blob.IsConfigured);
        }
    }
}
