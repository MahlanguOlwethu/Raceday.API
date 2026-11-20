using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using RaceDay.API.Models;
using RaceDay.API.Services;
using RaceDayAPI.Controllers;
using Xunit;

namespace RaceDay.API.Tests.Controllers
{
    public class EventManagementTests
    {
        // No image is ever uploaded in these tests, so an empty config is enough (no Azure call is made)
        private static EventsController CreateController(RaceDay.API.Data.RaceDayDbContext context, string userId, string? role)
        {
            var blob = new BlobService(new ConfigurationBuilder().AddInMemoryCollection().Build());
            var controller = new EventsController(context, blob);
            SessionMockHelper.SetSessionData(controller, userId, role!);
            return controller;
        }

        private static CreateEventDto Dto(string name) => new()
        {
            Name = name,
            Description = "Updated description",
            EventDate = DateTime.UtcNow.AddDays(30),
            Location = "Newlands",
            DistanceKm = 10m,
            EventType = "Cycle"
        };

        [Fact]
        public async Task UpdateEvent_OwnEvent_SavesChanges()
        {
            var context = TestHelpers.CreateSeededContext();
            var controller = CreateController(context, "1", "Organiser");

            var result = await controller.UpdateEvent(1, Dto("Renamed Run"), null);

            Assert.IsType<OkObjectResult>(result);
            var saved = await context.Events.SingleAsync();
            Assert.Equal("Renamed Run", saved.Name);
            Assert.Equal("Cycle", saved.EventType);
        }

        [Fact]
        public async Task UpdateEvent_AnotherOrganisersEvent_ReturnsForbidden()
        {
            var context = TestHelpers.CreateSeededContext();
            var controller = CreateController(context, "3", "Organiser");

            var result = await controller.UpdateEvent(1, Dto("Hijacked"), null);

            Assert.Equal(403, Assert.IsAssignableFrom<ObjectResult>(result).StatusCode);
            Assert.Equal("Park Run", (await context.Events.SingleAsync()).Name);
        }

        [Fact]
        public async Task UpdateEvent_UnknownEvent_ReturnsNotFound()
        {
            var controller = CreateController(TestHelpers.CreateSeededContext(), "1", "Organiser");

            var result = await controller.UpdateEvent(999, Dto("Nope"), null);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task DeleteEvent_OwnEventWithoutEnrolments_ReturnsNoContent()
        {
            var context = TestHelpers.CreateSeededContext();
            var controller = CreateController(context, "1", "Organiser");

            var result = await controller.DeleteEvent(1);

            Assert.IsType<NoContentResult>(result);
            Assert.Empty(context.Events);
        }

        [Fact]
        public async Task DeleteEvent_WithEnrolledParticipants_ReturnsConflictAndKeepsEvent()
        {
            var context = TestHelpers.CreateSeededContext();
            context.Enrolments.Add(new Enrolment { EventId = 1, CategoryId = 1, ParticipantId = 2, Status = "Pending" });
            await context.SaveChangesAsync();
            var controller = CreateController(context, "1", "Organiser");

            var result = await controller.DeleteEvent(1);

            Assert.IsType<ConflictObjectResult>(result);
            Assert.Single(context.Events);
        }

        [Fact]
        public async Task DeleteEvent_AnotherOrganisersEvent_ReturnsForbidden()
        {
            var context = TestHelpers.CreateSeededContext();
            var controller = CreateController(context, "3", "Organiser");

            var result = await controller.DeleteEvent(1);

            Assert.Equal(403, Assert.IsAssignableFrom<ObjectResult>(result).StatusCode);
            Assert.Single(context.Events);
        }

        [Fact]
        public async Task GetEvent_ReturnsEventWithCategories_WithoutLeakingOrganiserDetails()
        {
            var context = TestHelpers.CreateSeededContext();
            var controller = CreateController(context, "2", "Participant");

            var result = await controller.GetEvent(1);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("Park Run", TestHelpers.Prop(ok.Value, "Name"));
            Assert.Equal("Olive Organiser", TestHelpers.Prop(ok.Value, "OrganiserName"));
            Assert.Null(ok.Value!.GetType().GetProperty("Organiser"));       // no User entity / PasswordHash in the payload
            var categories = Assert.IsAssignableFrom<System.Collections.IEnumerable>(TestHelpers.Prop(ok.Value, "Categories"));
            Assert.Single(categories.Cast<object>());
        }

        // ---- Role enforcement on the real event endpoints ----

        [Theory]
        [InlineData(nameof(EventsController.CreateEvent))]
        [InlineData(nameof(EventsController.UpdateEvent))]
        [InlineData(nameof(EventsController.DeleteEvent))]
        public void OrganiserOnlyEndpoints_RejectParticipants(string action)
        {
            var controller = CreateController(TestHelpers.CreateSeededContext(), "2", "Participant");

            var result = TestHelpers.RunAuthorizeRoleFilter(controller, action);

            Assert.Equal(403, Assert.IsAssignableFrom<ObjectResult>(result).StatusCode);
        }

        [Theory]
        [InlineData(nameof(EventsController.GetAllEvents))]
        [InlineData(nameof(EventsController.CreateEvent))]
        [InlineData(nameof(EventsController.DeleteEvent))]
        public void EventEndpoints_RejectUnauthenticatedRequests(string action)
        {
            var controller = CreateController(TestHelpers.CreateSeededContext(), null!, null);

            var result = TestHelpers.RunAuthorizeRoleFilter(controller, action);

            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public void GetAllEvents_IsOpenToBothRoles()
        {
            foreach (var (id, role) in new[] { ("1", "Organiser"), ("2", "Participant") })
            {
                var controller = CreateController(TestHelpers.CreateSeededContext(), id, role);
                Assert.Null(TestHelpers.RunAuthorizeRoleFilter(controller, nameof(EventsController.GetAllEvents)));
            }
        }
    }
}
