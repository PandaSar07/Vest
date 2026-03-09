using System.ComponentModel.DataAnnotations;

namespace Vest.Models;

public class ForgotPasswordViewModel
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}
