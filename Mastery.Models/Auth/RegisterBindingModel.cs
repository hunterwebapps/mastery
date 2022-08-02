using System.ComponentModel.DataAnnotations;

namespace Mastery.Models.Auth;

public class RegisterBindingModel
{
    [Required]
    public string Username { get; set; } = default!;
    [Required]
    public string Password { get; set; } = default!;
}
