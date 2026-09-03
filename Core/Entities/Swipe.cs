namespace Core.Entities;


public class Swipe
{
    public Guid Id { get; set; }
    public Guid RoomId { get; set; }
    public Room Room { get; set; } = null!;

    public Guid ParticipantId { get; set; }
    public Guid? UserId { get; set; }
    public string? GuestName { get; set; }

    public int TmdbMovieId { get; set; }
    public SwipeDirection Direction { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}