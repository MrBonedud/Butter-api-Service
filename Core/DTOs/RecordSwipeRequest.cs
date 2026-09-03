using Core.Entities;

namespace Core.DTOs;

public class RecordSwipeRequest
{
    public Guid ParticipantId { get; set; }

    public string SessionToken { get; set; } = string.Empty;

    public int TmdbMovieId { get; set; }

    public SwipeDirection Direction { get; set; }
}