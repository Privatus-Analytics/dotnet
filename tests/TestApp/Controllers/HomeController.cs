using Microsoft.AspNetCore.Mvc;

namespace TestApp.Controllers;

public sealed class HomeController : Controller
{
    [HttpGet("/")]
    public IActionResult Index() => View();

    [HttpGet("/tag-overrides")]
    public IActionResult TagOverrides() => View();

    [HttpGet("/html-helpers")]
    public IActionResult HtmlHelpers() => View();
}
