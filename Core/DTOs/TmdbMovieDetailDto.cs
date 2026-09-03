namespace Core.DTOs;

public class TmdbMovieDetailDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Overview { get; set; } = string.Empty;
    public string? PosterPath { get; set; }
    public string? ReleaseDate { get; set; }
    public int Runtime { get; set; }
    public double VoteAverage { get; set; }
    public List<string> Genres { get; set; } = new();
}
