namespace Core.Entities;

public class Room
{
    public Guid Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public Guid CreatorId { get; set; }

    public User Creator { get; set; } = null!;

    public int? GenreId { get; set; }

    public int? MaxRuntimeMinutes { get; set; }

    public int? DecadeStart { get; set; }

    public int? CurrentMovieId { get; set; }

    public RoomStatus Status { get; set; } = RoomStatus.Waiting;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime LastActivityAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? ClosedAtUtc { get; set; }

    public ICollection<Swipe> Swipes { get; set; } = new List<Swipe>();
}