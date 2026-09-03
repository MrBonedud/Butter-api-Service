namespace Core.DTOs;

public class RoomDetailsResponse
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public Guid CreatorId { get; set; }
    public int? GenreId { get; set; }
    public int? MaxRuntimeMinutes { get; set; }
    public int? DecadeStart { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime LastActivityAtUtc { get; set; }
    public List<RoomGuestResponse> Guests { get; set; } = new();
}