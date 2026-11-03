using Lab5.Models;
using Lab5.Services;
using Lab5.Filters;
using Lab5.Middleware;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Lab5.Controllers;

[ResponseCache(
    NoStore = true,
    Location = ResponseCacheLocation.None)]
public class AuthController : Controller
{
    private readonly AccessStore _store;
    private readonly IPasswordHasher<AccessUser> _hasher;

    public AuthController(
        AccessStore store,
        IPasswordHasher<AccessUser> hasher)
    {
        _store = store;
        _hasher = hasher;
    }

    [HttpGet]
    public IActionResult Login()
    {
        return View(new AccessLoginModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(
        AccessLoginModel model)
    {
        model.Login = model.Login?.Trim() ?? "";

        if (!ModelState.IsValid)
        {
        HttpContext.Items[
            SecurityLoggingMiddleware.EventKey] =
            "login.validation_failed";

            return View(model);
        }

        var account = await _store.FindLoginAsync(model.Login);

        var validPassword = false;

        if (account != null)
        {
            if (!string.IsNullOrEmpty(account.PasswordHash))
            {
                var result = _hasher.VerifyHashedPassword(
                    account.User,
                    account.PasswordHash,
                    model.Password);

                validPassword =
                    result != PasswordVerificationResult.Failed;
            }
            else
            {
                validPassword =
                    account.Password != null &&
                    account.Password == model.Password;
            }
        }

        if (!validPassword || account == null)
        {
            HttpContext.Items[
                SecurityLoggingMiddleware.EventKey] =
                "login.failed";

            ModelState.AddModelError(
                "",
                "Неверный логин или пароль.");

            return View(model);
        }

        await _store.StartSessionAsync(
            HttpContext,
            account.User.Id);

        HttpContext.Items[
            PermissionFilter.CurrentUserKey] = account.User;

        HttpContext.Items[
            SecurityLoggingMiddleware.EventKey] =
            "login.succeeded";

        return account.User.Role switch
        {
            "admin" => RedirectToAction("Index", "Admin"),
            "manager" => RedirectToAction("Index", "Manager"),
            _ => RedirectToAction(nameof(NoAccess))
        };
    }

    [HttpGet]
    public IActionResult NoAccess()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        var user = await _store.GetCurrentUserAsync(HttpContext);

        if (user != null)
        {
            HttpContext.Items[
                PermissionFilter.CurrentUserKey] = user;
        }

        await _store.EndSessionAsync(HttpContext);

        HttpContext.Items[
            SecurityLoggingMiddleware.EventKey] =
            user == null ? "logout.anonymous" : "logout.succeeded";

        return RedirectToAction(nameof(Login));
    }
}