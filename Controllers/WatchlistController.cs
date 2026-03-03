using Microsoft.AspNetCore.Mvc;

namespace Vest.Controllers
{
    public class WatchlistController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
