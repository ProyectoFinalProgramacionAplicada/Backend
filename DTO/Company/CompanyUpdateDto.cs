namespace TruekAppAPI.DTO.Company;

public class CompanyUpdateDto
{
    public string? Description { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public double? Lat { get; set; }
    public double? Lng { get; set; }
    public bool? IsActive { get; set; }
}