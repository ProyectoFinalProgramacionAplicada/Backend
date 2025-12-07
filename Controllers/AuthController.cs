using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using TruekAppAPI.Data;
using TruekAppAPI.DTO.Auth;
using TruekAppAPI.Models;
using TruekAppAPI.Services;
using System.Security.Claims;
using System.Security.Cryptography;

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

        // ------------------------------------------------------------
        // REGISTER
        // ------------------------------------------------------------
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register(UserRegisterDto dto)
        {
            if (await _db.Users.AnyAsync(x => x.Email == dto.Email))
                return BadRequest("El correo ya está registrado.");
            
            // Validar teléfono duplicado
            if (!string.IsNullOrEmpty(dto.Phone))
            {
                var phoneExists = await _db.Users
                    .AnyAsync(u => u.Phone != null && u.Phone == dto.Phone);

                if (phoneExists)
                    return BadRequest(new { message = "El número de teléfono ya está registrado." });
            }

            var user = new User
            {
                Email = dto.Email,
                PasswordHash = _passwordHasher.HashPassword(dto.Password),
                Phone = dto.Phone,
                DisplayName = dto.DisplayName,
                Role = dto.Role
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(Register), new { user.Id }, new
            {
                user.Id,
                user.Email,
                user.DisplayName
            });
        }

        // ------------------------------------------------------------
        // LOGIN
        // ------------------------------------------------------------
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<LoginResponseDto>> Login(UserLoginDto dto)
        {
            var user = await _db.Users.FirstOrDefaultAsync(x => x.Email == dto.Email);

            if (user == null || !_passwordHasher.VerifyPassword(user.PasswordHash, dto.Password))
                return Unauthorized("Credenciales inválidas.");

            // 🆕 Actualizar LastLoginAt
            user.LastLoginAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            var token = _jwtService.GenerateToken(user);

            return new LoginResponseDto
            {
                Token = token,
                User = new UserInfoDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    Role = user.Role,
                    CompanyId = user.CompanyId,
                    TrueCoinBalance = user.TrueCoinBalance,
                    DisplayName = user.DisplayName,
                    Phone = user.Phone,
                    AvatarUrl = user.AvatarUrl
                }
            };
        }

        // ------------------------------------------------------------
        // FORGOT PASSWORD
        // ------------------------------------------------------------
        [HttpPost("forgot-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordDto dto)
        {
            string? token = null; // <-- la declaras aquí

            var user = await _db.Users.FirstOrDefaultAsync(x => x.Email == dto.Email);

            if (user != null)
            {
                token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64))
                    .Replace("+", "").Replace("/", "").Replace("=", "");

                user.PasswordResetToken = token;
                user.PasswordResetTokenExpires = DateTime.UtcNow.AddHours(1);

                await _db.SaveChangesAsync();
            }

            return Ok(new
            {
                message = "Token generado correctamente.",
                token = token
            });
        }


        // ------------------------------------------------------------
        // VERIFY RESET TOKEN
        // ------------------------------------------------------------
        [HttpGet("verify-reset-token")]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyResetToken([FromQuery] string token)
        {
            var user = await _db.Users.FirstOrDefaultAsync(x =>
                x.PasswordResetToken == token &&
                x.PasswordResetTokenExpires > DateTime.UtcNow
            );

            return user == null
                ? BadRequest(new { message = "Token inválido o expirado." })
                : Ok(new { message = "Token válido.", email = user.Email });
        }

        // ------------------------------------------------------------
        // RESET PASSWORD
        // ------------------------------------------------------------
        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword(ResetPasswordDto dto)
        {
            var user = await _db.Users.FirstOrDefaultAsync(x =>
                x.PasswordResetToken == dto.Token &&
                x.PasswordResetTokenExpires > DateTime.UtcNow
            );

            if (user == null)
                return BadRequest(new { message = "Token inválido o expirado." });

            // 🔧 Hash correcto de TU interfaz personalizada
            user.PasswordHash = _passwordHasher.HashPassword(dto.NewPassword);

            // Limpiar token
            user.PasswordResetToken = null;
            user.PasswordResetTokenExpires = null;

            await _db.SaveChangesAsync();

            return Ok(new { message = "Contraseña actualizada correctamente." });
        }

        // ------------------------------------------------------------
        // ME
        // ------------------------------------------------------------
        [HttpGet("me")]
        [Authorize]
        public async Task<ActionResult<UserInfoDto>> Me()
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(idClaim, out var id))
                return Unauthorized();

            var user = await _db.Users.FindAsync(id);
            if (user == null) return NotFound();

            return new UserInfoDto
            {
                Id = user.Id,
                Email = user.Email,
                Role = user.Role,
                CompanyId = user.CompanyId,
                TrueCoinBalance = user.TrueCoinBalance,
                DisplayName = user.DisplayName,
                Phone = user.Phone,
                AvatarUrl = user.AvatarUrl
            };
        }
    }
}
