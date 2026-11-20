using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Raceday.API.Controllers;
using RaceDay.API.Data;
using RaceDay.API.Dtos;
using RaceDay.API.Models;
using Xunit;

namespace RaceDay.API.Tests.Controllers
{
    public class AuthControllerTests
    {
        private RaceDayDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<RaceDayDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var context = new RaceDayDbContext(options);

            // This forces EF Core to apply the HasData() seeding from OnModelCreating
            context.Database.EnsureCreated();

            return context;
        }

        [Fact]
        public async Task Register_ValidUser_ReturnsCreated()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var controller = new AuthController(context);
            var dto = new RegisterDto { FirstName = "John", LastName = "Doe", Email = "john@test.com", Password = "Password123!", Role = "Participant" };

            // Act
            var result = await controller.Register(dto);

            // Assert
            // IsAssignableFrom safely checks if it inherited from ObjectResult
            var objectResult = Assert.IsAssignableFrom<ObjectResult>(result);
            Assert.Equal(201, objectResult.StatusCode);
            Assert.Equal(1, await context.Users.CountAsync());
        }

        [Fact]
        public async Task Login_ValidCredentials_ReturnsOk()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var controller = new AuthController(context);

            // Login writes to HttpContext.Session, so the controller needs a (mock) session
            SessionMockHelper.SetSessionData(controller, "0", "Participant");

            var registerDto = new RegisterDto { FirstName = "Jane", LastName = "Doe", Email = "jane@test.com", Password = "Password123!", Role = "Participant" };
            await controller.Register(registerDto);

            var loginDto = new LoginDto { Email = "jane@test.com", Password = "Password123!" };

            // Act
            var result = await controller.Login(loginDto);

            // Assert
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task Login_InvalidPassword_ReturnsUnauthorized()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var controller = new AuthController(context);

            var registerDto = new RegisterDto { FirstName = "Jane", LastName = "Doe", Email = "jane@test.com", Password = "Password123!", Role = "Participant" };
            await controller.Register(registerDto);

            var loginDto = new LoginDto { Email = "jane@test.com", Password = "WrongPassword!" };

            // Act
            var result = await controller.Login(loginDto);

            // Assert
            Assert.IsType<UnauthorizedObjectResult>(result);
        }
    }
}
