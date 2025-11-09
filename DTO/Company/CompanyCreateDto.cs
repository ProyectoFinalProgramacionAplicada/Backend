using System.ComponentModel.DataAnnotations;

namespace TruekAppAPI.DTO.Company;

public class CompanyCreateDto
{
    [Required] public string Name { get; set; } = default!;
    [Required] public string OwnerName { get; set; } = default!;
    public string? Description { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public double? Lat { get; set; }
    public double? Lng { get; set; }
}