namespace Core.DTOs;

public class RoomMovieCandidateResponse
{
    public int MovieId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Overview { get; set; } = string.Empty;
    public string PosterUrl { get; set; } = string.Empty;
    public int ReleaseYear { get; set; }
    public double VoteAverage { get; set; }
}