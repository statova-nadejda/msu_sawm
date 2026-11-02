using System.ComponentModel.DataAnnotations;

namespace Lab5.Models;

public record AccessUser(
    int Id,
    string Login,
    string? Role);

public record LoginAccount(
    AccessUser User,
    string? Password,
    string? PasswordHash);

public class AccessLoginModel
{
    [Required(ErrorMessage = "Введите логин.")]
    [StringLength(100)]
    public string Login { get; set; } = "";

    [Required(ErrorMessage = "Введите пароль.")]
    public string Password { get; set; } = "";
}

public class EditAccountModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Введите логин.")]
    [StringLength(100)]
    public string Login { get; set; } = "";
}