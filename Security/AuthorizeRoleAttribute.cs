using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RaceDay.API.Security
{
    public class AuthorizeRoleAttribute : ActionFilterAttribute
    {
        private readonly string[] _roles;

        // Using params allows us to pass multiple roles: [AuthorizeRole("Organiser", "Participant")]
        public AuthorizeRoleAttribute(params string[] roles)
        {
            _roles = roles;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            // 1. Check if they are logged in at all
            var userId = context.HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                context.Result = new UnauthorizedObjectResult(new { message = "You must be logged in to access this resource." });
                return;
            }

            // 2. Check if they have the correct role
            var userRole = context.HttpContext.Session.GetString("UserRole");

            // If roles are specified in the attribute, check if the user's role is in the list
            if (_roles.Length > 0 && !_roles.Contains(userRole))
            {
                context.Result = new ObjectResult(new { message = "You do not have permission to access this resource." })
                {
                    StatusCode = 403 // 403 Forbidden
                };
                return;
            }

            base.OnActionExecuting(context);
        }
    }
}