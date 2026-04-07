using Microsoft.AspNetCore.Mvc;

namespace Vest.Controllers
{
    public class CryptoController : Controller
    {
        public IActionResult Index()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")))
                return RedirectToAction("Log", "Home");

            return View();
        }
    }
}
