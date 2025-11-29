using System.ComponentModel.DataAnnotations;

namespace TruekAppAPI.DTO.Auth
{
    public class UserUpdateDto
    {
        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
        public string DisplayName { get; set; } = string.Empty;
        
        [Required]
        [RegularExpression(@"^\+[1-9]\d{6,14}$", ErrorMessage = "Phone number must be in E.164 format (e.g. +5917XXXXXXX, +1234567890)")]
        public string Phone { get; set; } = string.Empty;
    }
}