using Lab5.Models;
using Lab5.Services;
using Microsoft.AspNetCore.Identity;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

var connectionString = builder.Configuration
    .GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Не задана строка подключения DefaultConnection.");

builder.Services.AddSingleton<NpgsqlDataSource>(
    _ => NpgsqlDataSource.Create(connectionString));

builder.Services.AddScoped<AccessStore>();

builder.Services.AddScoped<
    IPasswordHasher<AccessUser>,
    PasswordHasher<AccessUser>>();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}");

app.Run();