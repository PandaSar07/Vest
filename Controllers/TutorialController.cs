using Microsoft.AspNetCore.Mvc;

namespace Vest.Controllers;

public class TutorialController : Controller
{
    private readonly ILogger<TutorialController> _logger;

    public TutorialController(ILogger<TutorialController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        return RedirectToAction("Index", "Home");
    }
}
