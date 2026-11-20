using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Controllers;
using RaceDay.API.Models;
using Xunit;

namespace RaceDay.API.Tests.Controllers
{
    public class EnrolmentTests
    {
        private static EnrolmentsController CreateController(RaceDay.API.Data.RaceDayDbContext context, string userId, string? role)
        {
            var controller = new EnrolmentsController(context);
            SessionMockHelper.SetSessionData(controller, userId, role!);
            return controller;
        }

        [Fact]
        public async Task Enrol_AsParticipant_RecordsEnrolmentLinkingParticipantEventAndCategory()
        {
            var context = TestHelpers.CreateSeededContext();
            var controller = CreateController(context, "2", "Participant");

            var result = await controller.EnrolInEvent(1, new CreateEnrolmentDto { CategoryId = 1 });

            var created = Assert.IsAssignableFrom<ObjectResult>(result);
            Assert.Equal(201, created.StatusCode);

            var saved = await context.Enrolments.SingleAsync();
            Assert.Equal(2, saved.ParticipantId);
            Assert.Equal(1, saved.EventId);
            Assert.Equal(1, saved.CategoryId);
            Assert.Equal("Pending", saved.Status);
        }

        [Fact]
        public async Task Enrol_TwiceInSameEvent_ReturnsConflict()
        {
            var context = TestHelpers.CreateSeededContext();
            var controller = CreateController(context, "2", "Participant");

            await controller.EnrolInEvent(1, new CreateEnrolmentDto { CategoryId = 1 });
            var second = await controller.EnrolInEvent(1, new CreateEnrolmentDto { CategoryId = 1 });

            Assert.IsType<ConflictObjectResult>(second);
            Assert.Equal(1, await context.Enrolments.CountAsync());
        }

        [Fact]
        public async Task Enrol_CategoryBelongingToAnotherEvent_ReturnsBadRequest()
        {
            var context = TestHelpers.CreateSeededContext();
            context.Events.Add(new Event { EventId = 2, OrganiserId = 1, Name = "Other", Description = "d", EventDate = DateTime.UtcNow.AddDays(5), Location = "x", DistanceKm = 10m, EventType = "Run" });
            context.Categories.Add(new Category { CategoryId = 2, EventId = 2, Name = "10km", CategoryType = "Distance", DistanceKm = 10m });
            await context.SaveChangesAsync();
            var controller = CreateController(context, "2", "Participant");

            // Category 2 belongs to event 2, not event 1
            var result = await controller.EnrolInEvent(1, new CreateEnrolmentDto { CategoryId = 2 });

            Assert.IsType<BadRequestObjectResult>(result);
            Assert.Empty(context.Enrolments);
        }

        [Fact]
        public async Task Enrol_UnknownEvent_ReturnsNotFound()
        {
            var context = TestHelpers.CreateSeededContext();
            var controller = CreateController(context, "2", "Participant");

            var result = await controller.EnrolInEvent(999, new CreateEnrolmentDto { CategoryId = 1 });

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task Enrol_AfterWithdrawing_ReactivatesTheCancelledEnrolment()
        {
            var context = TestHelpers.CreateSeededContext();
            var controller = CreateController(context, "2", "Participant");

            await controller.EnrolInEvent(1, new CreateEnrolmentDto { CategoryId = 1 });
            var enrolmentId = (await context.Enrolments.SingleAsync()).EnrolmentId;
            await controller.WithdrawEnrolment(enrolmentId);

            var result = await controller.EnrolInEvent(1, new CreateEnrolmentDto { CategoryId = 1 });

            Assert.Equal(201, Assert.IsAssignableFrom<ObjectResult>(result).StatusCode);
            var saved = await context.Enrolments.SingleAsync();
            Assert.Equal("Pending", saved.Status);
        }

        [Fact]
        public async Task GetMyEnrolments_ReturnsOnlyTheLoggedInParticipantsEnrolments()
        {
            var context = TestHelpers.CreateSeededContext();
            context.Users.Add(new User { UserId = 4, RoleId = 2, FirstName = "Sam", LastName = "Other", Email = "sam@test.com", PasswordHash = "x" });
            context.Enrolments.AddRange(
                new Enrolment { EventId = 1, CategoryId = 1, ParticipantId = 2, Status = "Pending" },
                new Enrolment { EventId = 1, CategoryId = 1, ParticipantId = 4, Status = "Pending" });
            await context.SaveChangesAsync();
            var controller = CreateController(context, "2", "Participant");

            var result = await controller.GetMyEnrolments();

            var ok = Assert.IsType<OkObjectResult>(result);
            var list = Assert.IsAssignableFrom<System.Collections.IEnumerable>(ok.Value);
            Assert.Single(list.Cast<object>());
        }

        [Fact]
        public async Task Organiser_CannotViewEnrolmentsForAnotherOrganisersEvent()
        {
            var context = TestHelpers.CreateSeededContext();
            var controller = CreateController(context, "3", "Organiser");   // organiser 3 does not own event 1

            var result = await controller.GetEventEnrolments(1);

            Assert.Equal(403, Assert.IsAssignableFrom<ObjectResult>(result).StatusCode);
        }

        [Fact]
        public async Task Organiser_CanViewEnrolmentsForOwnEvent()
        {
            var context = TestHelpers.CreateSeededContext();
            context.Enrolments.Add(new Enrolment { EventId = 1, CategoryId = 1, ParticipantId = 2, Status = "Pending" });
            await context.SaveChangesAsync();
            var controller = CreateController(context, "1", "Organiser");

            var result = await controller.GetEventEnrolments(1);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Single(Assert.IsAssignableFrom<System.Collections.IEnumerable>(ok.Value).Cast<object>());
        }

        // ---- Role enforcement on the real enrolment endpoints ----

        [Fact]
        public void EnrolInEvent_AsOrganiser_IsRejectedWith403()
        {
            var controller = CreateController(TestHelpers.CreateSeededContext(), "1", "Organiser");

            var result = TestHelpers.RunAuthorizeRoleFilter(controller, nameof(EnrolmentsController.EnrolInEvent));

            Assert.Equal(403, Assert.IsAssignableFrom<ObjectResult>(result).StatusCode);
        }

        [Fact]
        public void GetEventEnrolments_AsParticipant_IsRejectedWith403()
        {
            var controller = CreateController(TestHelpers.CreateSeededContext(), "2", "Participant");

            var result = TestHelpers.RunAuthorizeRoleFilter(controller, nameof(EnrolmentsController.GetEventEnrolments));

            Assert.Equal(403, Assert.IsAssignableFrom<ObjectResult>(result).StatusCode);
        }

        [Fact]
        public void EnrolInEvent_WhenNotLoggedIn_IsRejectedWith401()
        {
            var controller = new EnrolmentsController(TestHelpers.CreateSeededContext());
            SessionMockHelper.SetSessionData(controller, null!, null!);

            var result = TestHelpers.RunAuthorizeRoleFilter(controller, nameof(EnrolmentsController.EnrolInEvent));

            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public void EnrolInEvent_AsParticipant_IsAllowedByRoleFilter()
        {
            var controller = CreateController(TestHelpers.CreateSeededContext(), "2", "Participant");

            var result = TestHelpers.RunAuthorizeRoleFilter(controller, nameof(EnrolmentsController.EnrolInEvent));

            Assert.Null(result);
        }
    }
}
