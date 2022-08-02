using System.ComponentModel.DataAnnotations;

namespace Mastery.Models.Auth;
public class LoginBindingModel
{
    [Required]
    public string Username { get; set; } = default!;
    [Required]
    public string Password { get; set; } = default!;
    public bool RememberMe { get; set; } = false;
}
