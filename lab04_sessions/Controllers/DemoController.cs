using Microsoft.AspNetCore.Mvc;

namespace Lab5.Controllers;

public class DemoController : Controller
{
    private readonly IWebHostEnvironment _environment;

    public DemoController(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    [HttpGet("/Demo/Admin")]
    public IActionResult Admin()
    {
        if (!_environment.IsDevelopment())
        {
            return NotFound();
        }

        return Content(
            "Незащищённая демонстрационная панель. " +
            "Она открылась по прямой ссылке без входа.");
    }
}