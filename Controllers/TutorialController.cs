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
        ViewData["Title"] = "Tutorial | Vest";
        ViewData["FluidMain"] = true; // Use the same fluid layout as the homepage
        
        // Explicitly returning the view we created earlier 
        // (if you move the file to Views/Tutorial/Index.cshtml, you can just return View())
        return View("~/Views/Home/Tutorial.cshtml");
    }
}
