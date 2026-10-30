using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using XssLab.Data;
using XssLab.Models;

namespace XssLab.Controllers;

[Route("Account")]
public class AccountController : Controller
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher<AppUser> _hasher;

    private static readonly HashSet<string> CommonPasswords =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "123456",
            "12345678",
            "password",
            "qwerty",
            "Password1!",
            "Qwerty123!"
        };

    public AccountController(
        AppDbContext db,
        IPasswordHasher<AppUser> hasher)
    {
        _db = db;
        _hasher = hasher;
    }

    [HttpGet("")]
    public IActionResult Index()
    {
        return View();
    }

    [HttpPost("Register")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(
        string? login,
        string? password)
    {
        login = login?.Trim();

        if (string.IsNullOrWhiteSpace(login) || login.Length > 100)
        {
            return Show("Введите логин длиной от 1 до 100 символов.");
        }

        var error = ValidatePassword(password);

        if (error != null)
        {
            return Show(error);
        }

        if (await _db.Users.AnyAsync(x => x.Login == login))
        {
            return Show("Такой логин уже существует.");
        }

        var user = new AppUser
        {
            Login = login,
            Password = null
        };

        user.PasswordHash = _hasher.HashPassword(user, password!);

        _db.Users.Add(user);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is PostgresException
            { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            return Show("Такой логин уже существует.");
        }

        return Show("Регистрация успешна. В базе сохранён хеш пароля.");
    }

    [HttpPost("Login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(
        string? login,
        string? password)
    {
        if (string.IsNullOrWhiteSpace(login) ||
            string.IsNullOrEmpty(password))
        {
            return Show("Введите логин и пароль.");
        }

        login = login.Trim();

        var user = await _db.Users
            .SingleOrDefaultAsync(x => x.Login == login);

        if (user == null)
        {
            return Show("Неверный логин или пароль.");
        }

        if (user.PasswordHash == null)
        {
            return Show(
                "Для старых записей сначала выполните преобразование паролей.");
        }

        var result = _hasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            password);

        if (result == PasswordVerificationResult.Failed)
        {
            return Show("Неверный логин или пароль.");
        }

        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = _hasher.HashPassword(user, password);
            await _db.SaveChangesAsync();
        }

        return Show("Пароль верный. Проверка входа пройдена.");
    }

    [HttpPost("ConvertPasswords")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConvertPasswords()
    {
        var users = await _db.Users
            .Where(x => x.Password != null)
            .ToListAsync();

        foreach (var user in users)
        {
            if (user.PasswordHash == null)
            {
                user.PasswordHash = _hasher.HashPassword(
                    user,
                    user.Password!);
            }

            user.Password = null;
        }

        await _db.SaveChangesAsync();

        return Show(
            $"Обработано записей: {users.Count}. Открытые пароли удалены.");
    }

    private static string? ValidatePassword(string? password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            return "Введите пароль.";
        }

        if (CommonPasswords.Contains(password))
        {
            return "Этот пароль находится в списке распространённых.";
        }

        if (password.Length < 8 || password.Length > 128)
        {
            return "Пароль должен содержать от 8 до 128 символов.";
        }

        if (!password.Any(char.IsUpper))
        {
            return "Пароль должен содержать хотя бы одну большую букву.";
        }

        if (!password.Any(char.IsLower))
        {
            return "Пароль должен содержать хотя бы одну маленькую букву.";
        }

        if (!password.Any(char.IsDigit))
        {
            return "Пароль должен содержать хотя бы одну цифру.";
        }

        if (!password.Any(c =>
            !char.IsLetterOrDigit(c) && !char.IsWhiteSpace(c)))
        {
            return "Пароль должен содержать хотя бы один спецсимвол.";
        }

        return null;
    }

    private IActionResult Show(string message)
    {
        TempData["Message"] = message;

        return RedirectToAction(nameof(Index));
    }
}