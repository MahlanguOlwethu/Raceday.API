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
    }
}