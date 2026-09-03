namespace Core.DTOs;

public class CreateRoomResponse
{
    public RoomResponse Room { get; set; } = new();

    public Guid ParticipantId { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public string SessionToken { get; set; } = string.Empty;

    public DateTime JoinedAtUtc { get; set; }
}