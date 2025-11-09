using BCrypt.Net;

namespace TruekAppAPI.Services
{
    public class BcryptPasswordHasher : IPasswordHasher
    {
        public string HashPassword(string password) => BCrypt.Net.BCrypt.HashPassword(password);

        public bool VerifyPassword(string hashedPassword, string providedPassword) =>
            BCrypt.Net.BCrypt.Verify(providedPassword, hashedPassword);
    }
}
