using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using TruekAppAPI.Data;
using TruekAppAPI.DTO.Company;
using TruekAppAPI.Models;

namespace TruekAppAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CompaniesController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<CompanyDto>>> GetAll()
    {
        var companies = await db.Companies
            .Select(c => new CompanyDto
            {
                Id = c.Id,
                Name = c.Name,
                OwnerName = c.OwnerName,
                Description = c.Description,
                IsActive = c.IsActive
            }).ToListAsync();

        return Ok(companies);
    }

    [HttpPost]
    [Authorize(Roles = nameof(AppRole.Admin))]
    public async Task<IActionResult> Create(CompanyCreateDto dto)
    {
        var company = new Company
        {
            Name = dto.Name,
            OwnerName = dto.OwnerName,
            Description = dto.Description,
            Phone = dto.Phone,
            Address = dto.Address,
            Lat = dto.Lat,
            Lng = dto.Lng
        };

        db.Companies.Add(company);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(Create), new { company.Id }, company);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = nameof(AppRole.Admin))]
    public async Task<IActionResult> Update(int id, CompanyUpdateDto dto)
    {
        var company = await db.Companies.FindAsync(id);
        if (company == null) return NotFound();

        company.Description = dto.Description ?? company.Description;
        company.Phone = dto.Phone ?? company.Phone;
        company.Address = dto.Address ?? company.Address;
        company.IsActive = dto.IsActive ?? company.IsActive;
        await db.SaveChangesAsync();

        return NoContent();
    }
}