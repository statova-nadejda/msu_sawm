using Lab5.Filters;
using Lab5.Models;
using Lab5.Services;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace Lab5.Controllers;

public class AccountsController : Controller
{
    private readonly AccessStore _store;

    public AccountsController(AccessStore store)
    {
        _store = store;
    }

    private AccessUser CurrentUser =>
        (AccessUser)HttpContext.Items[
            PermissionFilter.CurrentUserKey]!;

    [HttpGet]
    [RequirePermission("accounts.view")]
    public async Task<IActionResult> Index()
    {
        ViewData["CurrentRole"] = CurrentUser.Role;

        return View(await _store.GetAccountsAsync());
    }

    [HttpGet]
    [RequirePermission("accounts.edit")]
    public async Task<IActionResult> Edit(int id)
    {
        var account = await _store.GetAccountAsync(id);

        if (account == null)
        {
            return NotFound();
        }

        if (CurrentUser.Role != "admin" && account.Role != null)
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        return View(new EditAccountModel
        {
            Id = account.Id,
            Login = account.Login
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission("accounts.edit")]
    public async Task<IActionResult> Edit(
        int id,
        EditAccountModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        var account = await _store.GetAccountAsync(id);

        if (account == null)
        {
            return NotFound();
        }

        var isAdmin = CurrentUser.Role == "admin";

        if (!isAdmin && account.Role != null)
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        model.Login = model.Login?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(model.Login))
        {
            ModelState.AddModelError(
                nameof(model.Login),
                "Логин не должен состоять из пробелов.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var updated = await _store.UpdateLoginAsync(
                id,
                model.Login,
                isAdmin);

            if (!updated)
            {
                return StatusCode(
                    StatusCodes.Status403Forbidden);
            }
        }
        catch (PostgresException ex)
            when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            ModelState.AddModelError(
                nameof(model.Login),
                "Такой логин уже существует.");

            return View(model);
        }

        return RedirectToAction(nameof(Index));
    }
}