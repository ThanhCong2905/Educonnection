using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using KetNoiGiaSu.Models;

namespace KetNoiGiaSu.Controllers;

public class HomeController : Controller
{
    public IActionResult Information(string section = "about")
    {
        if (!new[] { "about", "terms", "privacy", "contact" }.Contains(section)) return NotFound();
        ViewData["Section"] = section;
        return View();
    }
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
