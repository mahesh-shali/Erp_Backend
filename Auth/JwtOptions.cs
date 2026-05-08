namespace Erp.Api.Auth;

public sealed class JwtOptions
{
    public string Issuer { get; set; } = "Erp.Api";
    public string Audience { get; set; } = "Erp.Frontend";
    public string SigningKey { get; set; } = "replace-this-development-key-with-a-long-secret";
    public int ExpirationMinutes { get; set; } = 120;
}
