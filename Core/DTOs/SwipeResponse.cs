using Core.Entities;

namespace Core.DTOs;

public class SwipeResponse
{
    public Guid Id { get; set; }

    public Guid ParticipantId { get; set; }

    public int TmdbMovieId { get; set; }

    public SwipeDirection Direction { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public bool IsMatch { get; set; }
}