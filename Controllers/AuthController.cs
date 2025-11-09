using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using TruekAppAPI.Data;
using TruekAppAPI.DTO.Auth;
using TruekAppAPI.Models;
using TruekAppAPI.Services;
using System.Security.Claims;

namespace TruekAppAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IJwtService _jwtService;
        private readonly IPasswordHasher _passwordHasher;

        public AuthController(AppDbContext db, IJwtService jwtService, IPasswordHasher passwordHasher)
        {
            _db = db;
            _jwtService = jwtService;
            _passwordHasher = passwordHasher;
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register(UserRegisterDto dto)
        {
            if (await _db.Users.AnyAsync(x => x.Email == dto.Email))
                return BadRequest("El correo ya está registrado.");

            var user = new User
            {
                Email = dto.Email,
                PasswordHash = _passwordHasher.HashPassword(dto.Password),
                Phone = dto.Phone,
                Role = dto.Role
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(Register), new { user.Id }, new { user.Id, user.Email });
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<TokenDto>> Login(UserLoginDto dto)
        {
            var user = await _db.Users.FirstOrDefaultAsync(x => x.Email == dto.Email);
            if (user == null || !_passwordHasher.VerifyPassword(user.PasswordHash, dto.Password))
                return Unauthorized("Credenciales inválidas.");

            var token = _jwtService.GenerateToken(user);
            return new TokenDto { Token = token };
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<ActionResult<UserInfoDto>> Me()
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(idClaim, out var id))
            {
                return Unauthorized(); // El claim no es válido o no está presente
            }

            var user = await _db.Users.FindAsync(id);
            if (user == null) return NotFound();

            return new UserInfoDto
            {
                Id = user.Id,
                Email = user.Email,
                Role = user.Role,
                CompanyId = user.CompanyId,
                TrueCoinBalance = user.TrueCoinBalance
                // Agrega aquí otros campos si quieres
            };
        }
    }
}
