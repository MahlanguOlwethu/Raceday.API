using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using RaceDay.API.Data;
using RaceDay.API.Models;
using RaceDay.API.Security;
using RaceDay.API.Services;
using RaceDayAPI.Controllers;
using Xunit;

namespace RaceDay.API.Tests.Controllers
{
    public class EventsControllerTests
    {
        private RaceDayDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<RaceDayDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new RaceDayDbContext(options);
        }

        // BlobService needs an IConfiguration to be constructed. These tests never
        // upload an image (BannerImage is null), so an empty in-memory config is enough
        // and no real Azure connection is made.
        private BlobService GetBlobService()
        {
            var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
            return new BlobService(config);
        }

        [Fact]
        public async Task CreateEvent_AsOrganiser_ReturnsCreated()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var controller = new EventsController(context, GetBlobService());

            // Mock the session so the controller thinks an Organiser is logged in
            SessionMockHelper.SetSessionData(controller, "1", "Organiser");

            var dto = new CreateEventDto { Name = "Test Run", EventType = "Run", DistanceKm = 5.0m, EventDate = DateTime.UtcNow.AddDays(10), Location = "Park", Description = "Desc" };

            // Act (no banner image uploaded)
            var result = await controller.CreateEvent(dto, null);

            // Assert
            var objectResult = result as ObjectResult;
            Assert.NotNull(objectResult);
            Assert.Equal(201, objectResult.StatusCode);
        }

        [Fact]
        public void AuthorizeRole_AsParticipant_ReturnsForbidden()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var controller = new EventsController(context, GetBlobService());

            // Mock session as Participant
            SessionMockHelper.SetSessionData(controller, "2", "Participant");

            var filter = new AuthorizeRoleAttribute("Organiser");
            var filterContext = new ActionExecutingContext(
                new ActionContext(controller.HttpContext, new RouteData(), new ActionDescriptor()),
                new List<IFilterMetadata>(),
                new Dictionary<string, object?>(),
                controller
            );

            // Act
            filter.OnActionExecuting(filterContext);

            // Assert
            var result = filterContext.Result as ObjectResult;
            Assert.NotNull(result);
            Assert.Equal(403, result.StatusCode); // 403 Forbidden
        }
    }
}