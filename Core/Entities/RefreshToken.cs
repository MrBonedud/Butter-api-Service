namespace Core.Entities;

public class RefreshToken
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string Token { get; set; } = string.Empty;

    public DateTime ExpiresAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? RevokedAtUtc { get; set; }

    public string? ReplacedByToken { get; set; }

    // Navigation to User
    public User User { get; set; } = null!;

    // Computed helper property
    public bool IsActive =>
        RevokedAtUtc == null && ExpiresAtUtc > DateTime.UtcNow;
}
