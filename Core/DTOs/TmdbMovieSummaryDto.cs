namespace Core.DTOs;

public class TmdbMovieSummaryDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Overview { get; set; } = string.Empty;
    public string? PosterPath { get; set; }
    public string? ReleaseDate { get; set; }
    public double VoteAverage { get; set; }
    public IReadOnlyList<string> Genres { get; set; } = Array.Empty<string>();
    public int? Runtime { get; set; }
}
