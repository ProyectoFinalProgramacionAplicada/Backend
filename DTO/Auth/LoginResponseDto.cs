namespace TruekAppAPI.DTO.Auth;

public class LoginResponseDto
{
    public string Token { get; set; } = string.Empty;
    public UserInfoDto User { get; set; } = default!;
}