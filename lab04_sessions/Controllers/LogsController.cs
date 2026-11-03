using Lab5.Filters;
using Lab5.Models;
using Lab5.Services;
using Microsoft.AspNetCore.Mvc;

namespace Lab5.Controllers;

public class LogsController : Controller
{
    private readonly SecurityLog _log;

    public LogsController(SecurityLog log)
    {
        _log = log;
    }

    [HttpGet]
    [RequirePermission("logs.view")]
    public async Task<IActionResult> Index()
    {
        var user = (AccessUser)HttpContext.Items[
            PermissionFilter.CurrentUserKey]!;

        if (user.Role != "admin")
        {
            return StatusCode(
                StatusCodes.Status403Forbidden);
        }

        try
        {
            return View(await _log.ReadRecentAsync());
        }
        catch (Exception ex)
            when (ex is IOException or UnauthorizedAccessException)
        {
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                "Журнал временно недоступен.");
        }
    }
}