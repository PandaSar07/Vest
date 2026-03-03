using Microsoft.AspNetCore.Mvc;

namespace Vest.Controllers;

public class ScreenerController : Controller
{
    public IActionResult Index() => View();
}
