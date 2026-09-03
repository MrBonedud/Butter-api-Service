namespace Core.DTOs;

public class RoomGuestResponse
{
    public Guid ParticipantId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public DateTime JoinedAtUtc { get; set; }
}
