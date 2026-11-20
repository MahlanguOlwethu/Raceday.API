using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using RaceDay.API.Controllers;
using RaceDay.API.Dtos;
using RaceDay.API.Services;
using Xunit;

namespace RaceDay.API.Tests.Controllers
{
    public class UserProfileTests
    {
        private static UsersController CreateController(RaceDay.API.Data.RaceDayDbContext context, string userId, string role)
        {
            var blob = new BlobService(new ConfigurationBuilder().AddInMemoryCollection().Build());
            var controller = new UsersController(context, blob);
            SessionMockHelper.SetSessionData(controller, userId, role);
            return controller;
        }

        [Theory]
        [InlineData("1", "Organiser")]
        [InlineData("2", "Participant")]
        public async Task UpdateProfile_BothRoles_CanUpdateTheirOwnDetails(string userId, string role)
        {
            var context = TestHelpers.CreateSeededContext();
            var controller = CreateController(context, userId, role);

            var result = await controller.UpdateProfile(new UpdateProfileDto { FirstName = "New", LastName = "Name", PhoneNumber = "0821234567" });

            Assert.IsType<OkObjectResult>(result);
            var saved = await context.Users.SingleAsync(u => u.UserId == int.Parse(userId));
            Assert.Equal("New", saved.FirstName);
            Assert.Equal("0821234567", saved.PhoneNumber);
        }

        [Fact]
        public async Task UpdateProfile_OnlyChangesTheLoggedInUser()
        {
            var context = TestHelpers.CreateSeededContext();
            var controller = CreateController(context, "2", "Participant");

            await controller.UpdateProfile(new UpdateProfileDto { FirstName = "Changed", LastName = "Me" });

            Assert.Equal("Olive", (await context.Users.SingleAsync(u => u.UserId == 1)).FirstName);
        }

        [Fact]
        public void UpdateProfile_WhenNotLoggedIn_IsRejectedWith401()
        {
            var controller = new UsersController(TestHelpers.CreateSeededContext(),
                new BlobService(new ConfigurationBuilder().AddInMemoryCollection().Build()));
            SessionMockHelper.SetSessionData(controller, null!, null!);

            var result = TestHelpers.RunAuthorizeRoleFilter(controller, nameof(UsersController.UpdateProfile));

            Assert.IsType<UnauthorizedObjectResult>(result);
        }
    }
}
