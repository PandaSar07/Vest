using Microsoft.AspNetCore.Mvc;

namespace Vest.Controllers
{
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            // Redirect to login if not authenticated
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")))
                return RedirectToAction("Log", "Home");

            return View();
        }

    }
}
