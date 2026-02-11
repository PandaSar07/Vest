using Microsoft.AspNetCore.Mvc;

namespace Vest.Controllers
{
    public class StocksController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
