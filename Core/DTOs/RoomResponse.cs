namespace Core.DTOs;

public class RoomResponse
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public Guid CreatorId { get; set; }
    public int? GenreId { get; set; }
    public int? MaxRuntimeMinutes { get; set; }
    public int? DecadeStart { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}