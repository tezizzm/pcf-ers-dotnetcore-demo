using Microsoft.AspNetCore.Mvc;

namespace Articulate.Controllers;

public class Echo : Controller
{
    // GET
    public IActionResult Index()
    {
        return View();
    }
}