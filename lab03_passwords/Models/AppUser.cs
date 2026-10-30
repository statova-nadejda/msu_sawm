namespace XssLab.Models;

public class AppUser
{
    public int Id { get; set; }

    public string Login { get; set; } = "";

    public string? Password { get; set; }

    public string? PasswordHash { get; set; }
}