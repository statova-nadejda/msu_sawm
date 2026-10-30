using System.ComponentModel.DataAnnotations;

namespace XssLab.Models;

public class GuestEntry
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string User { get; set; } = "";

    [Required]
    [StringLength(2000)]
    public string TextMessage { get; set; } = "";

    [Required]
    [EmailAddress]
    [StringLength(200)]
    public string Email { get; set; } = "";

    public DateTime DataTimeMessage { get; set; }
}