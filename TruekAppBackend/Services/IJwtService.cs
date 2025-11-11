using TruekAppBackend.Models;

namespace TruekAppBackend.Services
{
    public interface IJwtService
    {
        string GenerateToken(User user);
    }
}