using TruekAppAPI.Models;

namespace TruekAppAPI.Services
{
    public interface IJwtService
    {
        string GenerateToken(User user);
    }
}