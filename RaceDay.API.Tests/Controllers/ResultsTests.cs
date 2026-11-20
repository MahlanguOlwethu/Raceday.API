using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Controllers;
using RaceDay.API.Models;
using Xunit;

namespace RaceDay.API.Tests.Controllers
{
    public class ResultsTests
    {
        private static ResultsController CreateController(RaceDay.API.Data.RaceDayDbContext context, string userId, string role)
        {
            var controller = new ResultsController(context);
            SessionMockHelper.SetSessionData(controller, userId, role);
            return controller;
        }

        [Fact]
        public async Task CaptureResult_ForEnrolledParticipant_SavesFinishTimeAndPosition()
        {
            var context = TestHelpers.CreateSeededContext();
            context.Enrolments.Add(new Enrolment { EnrolmentId = 1, EventId = 1, CategoryId = 1, ParticipantId = 2, Status = "Confirmed" });
            await context.SaveChangesAsync();
            var controller = CreateController(context, "1", "Organiser");

            var result = await controller.CaptureResult(1, new CreateResultDto { EnrolmentId = 1, FinishTime = TimeSpan.FromMinutes(24), FinishPosition = 3 });

            Assert.Equal(201, Assert.IsAssignableFrom<ObjectResult>(result).StatusCode);
            var saved = await context.Results.SingleAsync();
            Assert.Equal(3, saved.FinishPosition);
            Assert.Equal(TimeSpan.FromMinutes(24), saved.FinishTime);
        }

        [Fact]
        public async Task CaptureResult_ForCancelledEnrolment_ReturnsBadRequest()
        {
            var context = TestHelpers.CreateSeededContext();
            context.Enrolments.Add(new Enrolment { EnrolmentId = 1, EventId = 1, CategoryId = 1, ParticipantId = 2, Status = "Cancelled" });
            await context.SaveChangesAsync();
            var controller = CreateController(context, "1", "Organiser");

            var result = await controller.CaptureResult(1, new CreateResultDto { EnrolmentId = 1, FinishTime = TimeSpan.FromMinutes(30), FinishPosition = 1 });

            Assert.IsType<BadRequestObjectResult>(result);
            Assert.Empty(context.Results);
        }

        [Fact]
        public async Task CaptureResult_DuplicatePositionInSameEvent_ReturnsConflict()
        {
            var context = TestHelpers.CreateSeededContext();
            context.Users.Add(new User { UserId = 4, RoleId = 2, FirstName = "Sam", LastName = "Two", Email = "sam@test.com", PasswordHash = "x" });
            context.Enrolments.AddRange(
                new Enrolment { EnrolmentId = 1, EventId = 1, CategoryId = 1, ParticipantId = 2, Status = "Confirmed" },
                new Enrolment { EnrolmentId = 2, EventId = 1, CategoryId = 1, ParticipantId = 4, Status = "Confirmed" });
            await context.SaveChangesAsync();
            var controller = CreateController(context, "1", "Organiser");

            await controller.CaptureResult(1, new CreateResultDto { EnrolmentId = 1, FinishTime = TimeSpan.FromMinutes(20), FinishPosition = 1 });
            var second = await controller.CaptureResult(1, new CreateResultDto { EnrolmentId = 2, FinishTime = TimeSpan.FromMinutes(21), FinishPosition = 1 });

            Assert.IsType<ConflictObjectResult>(second);
            Assert.Equal(1, await context.Results.CountAsync());
        }

        [Fact]
        public async Task CaptureResult_AnotherOrganisersEvent_ReturnsForbidden()
        {
            var context = TestHelpers.CreateSeededContext();
            context.Enrolments.Add(new Enrolment { EnrolmentId = 1, EventId = 1, CategoryId = 1, ParticipantId = 2, Status = "Confirmed" });
            await context.SaveChangesAsync();
            var controller = CreateController(context, "3", "Organiser");

            var result = await controller.CaptureResult(1, new CreateResultDto { EnrolmentId = 1, FinishTime = TimeSpan.FromMinutes(20), FinishPosition = 1 });

            Assert.Equal(403, Assert.IsAssignableFrom<ObjectResult>(result).StatusCode);
        }

        [Fact]
        public async Task GetMyResults_IncludesCategoryNamePositionAndTotalFinishers()
        {
            var context = TestHelpers.CreateSeededContext();
            context.Enrolments.Add(new Enrolment { EnrolmentId = 1, EventId = 1, CategoryId = 1, ParticipantId = 2, Status = "Confirmed" });
            context.Results.Add(new Result { EnrolmentId = 1, FinishTime = TimeSpan.FromMinutes(25), FinishPosition = 1 });
            await context.SaveChangesAsync();
            var controller = CreateController(context, "2", "Participant");

            var result = await controller.GetMyResults();

            var ok = Assert.IsType<OkObjectResult>(result);
            var first = Assert.IsAssignableFrom<System.Collections.IEnumerable>(ok.Value).Cast<object>().Single();
            Assert.Equal("5km Open", TestHelpers.Prop(first, "CategoryName"));   // the My Results page reads this field
            Assert.Equal(1, TestHelpers.Prop(first, "FinishPosition"));
            Assert.Equal(1, TestHelpers.Prop(first, "TotalFinishers"));
        }

        [Fact]
        public void CaptureResult_AsParticipant_IsRejectedWith403()
        {
            var controller = CreateController(TestHelpers.CreateSeededContext(), "2", "Participant");

            var result = TestHelpers.RunAuthorizeRoleFilter(controller, nameof(ResultsController.CaptureResult));

            Assert.Equal(403, Assert.IsAssignableFrom<ObjectResult>(result).StatusCode);
        }

        [Fact]
        public void GetMyResults_AsOrganiser_IsRejectedWith403()
        {
            var controller = CreateController(TestHelpers.CreateSeededContext(), "1", "Organiser");

            var result = TestHelpers.RunAuthorizeRoleFilter(controller, nameof(ResultsController.GetMyResults));

            Assert.Equal(403, Assert.IsAssignableFrom<ObjectResult>(result).StatusCode);
        }
    }
}
