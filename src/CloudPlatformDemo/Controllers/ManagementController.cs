using Microsoft.AspNetCore.Mvc;

namespace CloudPlatformDemo.Controllers;

public class ManagementController : Controller
{
    public IActionResult Index() => View();
}