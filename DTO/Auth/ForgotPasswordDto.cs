using System.ComponentModel.DataAnnotations;

namespace TruekAppAPI.DTO.Auth
{
    public class ForgotPasswordDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }
}