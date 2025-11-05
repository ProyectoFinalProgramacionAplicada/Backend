namespace TruekAppAPI.DTO.Company;

public class CompanyDto
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public string OwnerName { get; set; } = default!;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}