using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using SqlInjectionLab.Models;

namespace SqlInjectionLab.Controllers;

public class AuthController : Controller
{
    private readonly string _connectionString;
    private readonly IWebHostEnvironment _environment;

    public AuthController(
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        _connectionString = configuration
            .GetConnectionString("DefaultConnection")!;

        _environment = environment;
    }

    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VulnerableLogin(LoginModel model)
    {
        if (!_environment.IsDevelopment())
            return NotFound();

        await using var connection =
            new NpgsqlConnection(_connectionString);

        await connection.OpenAsync();

        string sql =
            $"SELECT id FROM \"user\" " +
            $"WHERE login = '{model.Login}' " +
            $"AND password = '{model.Password}'";

        await using var command = new NpgsqlCommand(sql, connection);

        var result = await command.ExecuteScalarAsync();

        if (result != null)
        {
            await SignIn(model.Login);
            return RedirectToAction("Index", "Admin");
        }

        ViewBag.Error = "Неверный логин или пароль";
        return View("Login");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SafeLogin(LoginModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Login) ||
            string.IsNullOrWhiteSpace(model.Password))
        {
            ViewBag.Error = "Заполните все поля";
            return View("Login");
        }

        if (model.Login.Length > 50 || model.Password.Length > 100)
        {
            ViewBag.Error = "Слишком длинные данные";
            return View("Login");
        }

        await using var connection =
            new NpgsqlConnection(_connectionString);

        await connection.OpenAsync();

        string sql =
            "SELECT id FROM \"user\" " +
            "WHERE login = $1 AND password = $2";

        await using var command = new NpgsqlCommand(sql, connection);

        command.Parameters.Add(
            new NpgsqlParameter { Value = model.Login });

        command.Parameters.Add(
            new NpgsqlParameter { Value = model.Password });

        var result = await command.ExecuteScalarAsync();

        if (result != null)
        {
            await SignIn(model.Login);
            return RedirectToAction("Index", "Admin");
        }

        ViewBag.Error = "Неверный логин или пароль";
        return View("Login");
    }

    private async Task SignIn(string login)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, login)
        };

        var identity = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme);

        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);

        return RedirectToAction("Login");
    }
}