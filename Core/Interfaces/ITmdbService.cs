using Core.DTOs;

namespace Core.Interfaces;

public interface ITmdbService
{
    Task<IReadOnlyList<TmdbMovieSummaryDto>> SearchMoviesAsync(string query, int page, CancellationToken cancellationToken);
    Task<TmdbMovieDetailDto?> GetMovieDetailAsync(int movieId, CancellationToken cancellationToken);
    Task<IReadOnlyList<TmdbCastMemberDto>> GetMovieCastAsync(int movieId, CancellationToken cancellationToken);
    Task<IReadOnlyList<TmdbMovieSummaryDto>> GetSimilarMoviesAsync(int movieId, int page, CancellationToken cancellationToken);

    Task<IReadOnlyList<TmdbMovieSummaryDto>> DiscoverMoviesAsync(int? genreId, int? maxRuntimeMinutes, int? decadeStart, int page, CancellationToken cancellationToken);
}
