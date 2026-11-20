using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.Models;
using RaceDay.API.Security;
using System.Reflection;

namespace RaceDay.API.Tests
{
    public static class TestHelpers
    {
        // Fresh in-memory database; EnsureCreated applies the seeded Organiser/Participant roles
        public static RaceDayDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<RaceDayDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            var context = new RaceDayDbContext(options);
            context.Database.EnsureCreated();
            return context;
        }

        // Seeds: Organiser (id 1), Organiser (id 3), Participant (id 2),
        // Event 1 owned by organiser 1 with Category 1.
        public static RaceDayDbContext CreateSeededContext()
        {
            var context = CreateContext();

            context.Users.AddRange(
                new User { UserId = 1, RoleId = 1, FirstName = "Olive", LastName = "Organiser", Email = "org1@test.com", PasswordHash = "x" },
                new User { UserId = 2, RoleId = 2, FirstName = "Pat", LastName = "Participant", Email = "pat@test.com", PasswordHash = "x" },
                new User { UserId = 3, RoleId = 1, FirstName = "Other", LastName = "Organiser", Email = "org2@test.com", PasswordHash = "x" });

            context.Events.Add(new Event
            {
                EventId = 1,
                OrganiserId = 1,
                Name = "Park Run",
                Description = "Weekly 5km",
                EventDate = DateTime.UtcNow.AddDays(14),
                Location = "Zoo Lake",
                DistanceKm = 5m,
                EventType = "Run"
            });

            context.Categories.Add(new Category { CategoryId = 1, EventId = 1, Name = "5km Open", CategoryType = "Distance", DistanceKm = 5m });

            context.SaveChanges();
            return context;
        }

        // Runs every [AuthorizeRole] filter declared on a real controller action, exactly as the
        // framework would before the action executes. Returns the short-circuit result (null = allowed).
        public static IActionResult? RunAuthorizeRoleFilter(ControllerBase controller, string actionName)
        {
            var method = controller.GetType().GetMethod(actionName)
                ?? throw new InvalidOperationException($"Action {actionName} not found");

            var filterContext = new ActionExecutingContext(
                new ActionContext(controller.HttpContext, new RouteData(), new ActionDescriptor()),
                new List<IFilterMetadata>(),
                new Dictionary<string, object?>(),
                controller);

            foreach (var attr in method.GetCustomAttributes<AuthorizeRoleAttribute>())
            {
                attr.OnActionExecuting(filterContext);
                if (filterContext.Result != null) break;
            }

            return filterContext.Result;
        }

        public static object? Prop(object? target, string name) =>
            target?.GetType().GetProperty(name)?.GetValue(target);
    }
}
