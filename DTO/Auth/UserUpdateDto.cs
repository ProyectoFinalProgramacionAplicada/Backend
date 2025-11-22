using System.ComponentModel.DataAnnotations;

namespace TruekAppAPI.DTO.Auth
{
    public class UserUpdateDto
    {
        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
        public string DisplayName { get; set; } = string.Empty;
        
        [Required]
        [RegularExpression(@"^\+[0-9]{1,3}\s?[0-9]{6,14}$", ErrorMessage = "El teléfono debe incluir código de país (ej. +591)")]
        public string Phone { get; set; } = string.Empty;
    }
}