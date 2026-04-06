using Microsoft.AspNetCore.Mvc;

namespace Vest.Controllers
{
    public class CryptoController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
