using System.ComponentModel.DataAnnotations;

namespace TruekAppAPI.DTO.Auth
{
    public class UserUpdateDto
    {
        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
        public string DisplayName { get; set; } = string.Empty;

        [Phone(ErrorMessage = "El formato del teléfono no es válido")]
        public string? Phone { get; set; }
    }
}