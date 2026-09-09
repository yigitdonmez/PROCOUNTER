namespace CalorieTracker.Shared;

public class AuthResponseDto
{
    public string Token { get; set; } = null!;
    public string UserId { get; set; } = null!;
    public DateTime ExpiresAtUtc { get; set; }
}