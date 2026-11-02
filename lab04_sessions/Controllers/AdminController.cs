using Lab5.Filters;
using Lab5.Models;
using Microsoft.AspNetCore.Mvc;

namespace Lab5.Controllers;

public class AdminController : Controller
{
    [RequirePermission("admin.panel")]
    public IActionResult Index()
    {
        var user = (AccessUser)HttpContext.Items[
            PermissionFilter.CurrentUserKey]!;

        return View(user);
    }
}