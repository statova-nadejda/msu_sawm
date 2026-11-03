using Lab5.Models;
using Lab5.Services;
using Lab5.Middleware;
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

builder.Services.AddSingleton<SecurityLog>();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseMiddleware<SecurityLoggingMiddleware>();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}");

app.Run();