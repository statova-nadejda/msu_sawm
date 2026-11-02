using Lab5.Models;
using Lab5.Services;
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
                // Совместимость с открытыми паролями из ЛР1.
                validPassword =
                    account.Password != null &&
                    account.Password == model.Password;
            }
        }

        if (!validPassword || account == null)
        {
            ModelState.AddModelError(
                "",
                "Неверный логин или пароль.");

            return View(model);
        }

        await _store.StartSessionAsync(
            HttpContext,
            account.User.Id);

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
        await _store.EndSessionAsync(HttpContext);

        return RedirectToAction(nameof(Login));
    }
}