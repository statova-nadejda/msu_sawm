using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using XssLab.Data;
using XssLab.Models;

namespace XssLab.Controllers;

[Route("Guest")]
public class GuestController : Controller
{
    private readonly AppDbContext _db;

    public GuestController(AppDbContext db)
    {
        _db = db;
    }

    // Начальная страница — защищённый вариант.
    [HttpGet("")]
    [HttpGet("Index")]
    public IActionResult Index()
    {
        return RedirectToAction(nameof(Safe));
    }

    [HttpGet("Vulnerable")]
    public async Task<IActionResult> Vulnerable()
    {
        return View(await GetEntries());
    }

    [HttpGet("Safe")]
    public async Task<IActionResult> Safe()
    {
        return View(await GetEntries());
    }

    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind("User,TextMessage,Email")] GuestEntry entry,
        string mode)
    {
        var viewName = mode == "vulnerable"
            ? nameof(Vulnerable)
            : nameof(Safe);

        if (string.IsNullOrWhiteSpace(entry.User))
        {
            ModelState.AddModelError(
                nameof(entry.User),
                "Введите имя.");
        }

        if (string.IsNullOrWhiteSpace(entry.TextMessage))
        {
            ModelState.AddModelError(
                nameof(entry.TextMessage),
                "Введите сообщение.");
        }

        if (!ModelState.IsValid)
        {
            return View(viewName, await GetEntries());
        }

        entry.DataTimeMessage = DateTime.UtcNow;

        _db.Entries.Add(entry);
        await _db.SaveChangesAsync();

        return RedirectToAction(viewName);
    }

    private Task<List<GuestEntry>> GetEntries()
    {
        return _db.Entries
            .AsNoTracking()
            .OrderByDescending(x => x.DataTimeMessage)
            .ThenByDescending(x => x.Id)
            .ToListAsync();
    }
}