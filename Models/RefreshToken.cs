namespace Erp.Api.Models;

public sealed class RefreshToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTimeOffset CreatedDate { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ExpiresDate { get; set; }
    public DateTimeOffset? RevokedDate { get; set; }
    public string? ReplacedByTokenHash { get; set; }

    public AppUser? User { get; set; }
    public bool IsActive => RevokedDate is null && ExpiresDate > DateTimeOffset.UtcNow;
}
