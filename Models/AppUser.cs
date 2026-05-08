namespace Erp.Api.Models;

public sealed class AppUser
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public int RoleId { get; set; }

    public AppRole? Role { get; set; }
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
}
