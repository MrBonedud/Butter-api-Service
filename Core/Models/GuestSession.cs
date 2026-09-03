namespace Core.Models;

public class GuestSession
{
    public Guid ParticipantId { get; set; }

    public string RoomCode { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string SessionToken { get; set; } = string.Empty;

    public DateTime JoinedAtUtc { get; set; }
}