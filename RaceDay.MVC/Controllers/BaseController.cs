using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RaceDay.MVC.Controllers
{
    public class BaseController : Controller
    {
        // Helper properties to check user status
        protected bool IsLoggedIn => !string.IsNullOrEmpty(HttpContext.Session.GetString("UserRole"));
        protected string CurrentUserRole => HttpContext.Session.GetString("UserRole") ?? "";
        protected string CurrentUserName => HttpContext.Session.GetString("UserName") ?? "";

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            base.OnActionExecuting(context);

            // Pass these variables to ViewBag so _Layout.cshtml can use them
            ViewBag.IsLoggedIn = IsLoggedIn;
            ViewBag.UserRole = CurrentUserRole;
            ViewBag.UserName = CurrentUserName;
        }

        public override void OnActionExecuted(ActionExecutedContext context)
        {
            base.OnActionExecuted(context);

            // Set by ApiSessionHandler when the API session has ended
            if (HttpContext.Items.ContainsKey("ApiSessionExpired"))
            {
                TempData["ErrorMessage"] = "Your session has expired. Please log in again.";
                context.Result = RedirectToAction("Login", "Auth");
            }
        }
    }
}