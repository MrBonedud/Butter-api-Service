namespace Core.DTOs;

public class RoomSettingsRequest
{
    public int? GenreId { get; set; }
    public int? MaxRuntimeMinutes { get; set; }
    public int? DecadeStart { get; set; }
}
