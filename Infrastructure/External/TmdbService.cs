using System.Text.Json;
using System.Text.Json.Serialization;
using Core.Common;
using Core.DTOs;
using Core.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Infrastructure.External;

public class TmdbService : ITmdbService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly TmdbOptions _options;

    public TmdbService(HttpClient httpClient, IMemoryCache cache, IOptions<TmdbOptions> options)
    {
        _httpClient = httpClient;
        _cache = cache;
        _options = options.Value;

        if (!string.IsNullOrWhiteSpace(_options.BaseUrl))
        {
            _httpClient.BaseAddress = new Uri(_options.BaseUrl);
        }
    }

    public async Task<IReadOnlyList<TmdbMovieSummaryDto>> SearchMoviesAsync(string query, int page, CancellationToken cancellationToken)
    {
        var cacheKey = $"tmdb:search:{query.Trim().ToLowerInvariant()}:{page}";

        return await GetOrCreateAsync(cacheKey, TimeSpan.FromMinutes(10), async () =>
        {
            var url = $"search/movie?api_key={GetApiKey()}&query={Uri.EscapeDataString(query)}&page={page}";
            var response = await GetFromJsonAsync<SearchMoviesResponse>(url, cancellationToken);

            return (IReadOnlyList<TmdbMovieSummaryDto>)(response?.Results?.Select(MapSummary).ToList()
                ?? new List<TmdbMovieSummaryDto>());
        });
    }

    public async Task<TmdbMovieDetailDto?> GetMovieDetailAsync(int movieId, CancellationToken cancellationToken)
    {
        var cacheKey = $"tmdb:detail:{movieId}";

        return await GetOrCreateAsync(cacheKey, TimeSpan.FromMinutes(30), async () =>
        {
            var url = $"movie/{movieId}?api_key={GetApiKey()}";
            var response = await GetFromJsonAsync<MovieDetailResponse>(url, cancellationToken);

            if (response is null)
            {
                return null;
            }

            return new TmdbMovieDetailDto
            {
                Id = response.Id,
                Title = response.Title ?? string.Empty,
                Overview = response.Overview ?? string.Empty,
                PosterPath = response.PosterPath,
                ReleaseDate = response.ReleaseDate,
                Runtime = response.Runtime,
                VoteAverage = response.VoteAverage,
                Genres = response.Genres?.Select(g => g.Name ?? string.Empty).Where(name => !string.IsNullOrWhiteSpace(name)).ToList()
                    ?? new List<string>()
            };
        });
    }

    public async Task<IReadOnlyList<TmdbCastMemberDto>> GetMovieCastAsync(int movieId, CancellationToken cancellationToken)
    {
        var cacheKey = $"tmdb:cast:{movieId}";

        return await GetOrCreateAsync(cacheKey, TimeSpan.FromMinutes(30), async () =>
        {
            var url = $"movie/{movieId}/credits?api_key={GetApiKey()}";
            var response = await GetFromJsonAsync<CreditsResponse>(url, cancellationToken);

            return (IReadOnlyList<TmdbCastMemberDto>)(response?.Cast?.OrderBy(c => c.Order).Take(15).Select(c => new TmdbCastMemberDto
            {
                Id = c.Id,
                Name = c.Name ?? string.Empty,
                Character = c.Character ?? string.Empty,
                ProfilePath = c.ProfilePath,
                Order = c.Order
            }).ToList() ?? new List<TmdbCastMemberDto>());
        });
    }

    public async Task<IReadOnlyList<TmdbMovieSummaryDto>> GetSimilarMoviesAsync(int movieId, int page, CancellationToken cancellationToken)
    {
        var cacheKey = $"tmdb:similar:{movieId}:{page}";

        return await GetOrCreateAsync(cacheKey, TimeSpan.FromMinutes(20), async () =>
        {
            var url = $"movie/{movieId}/similar?api_key={GetApiKey()}&page={page}";
            var response = await GetFromJsonAsync<SearchMoviesResponse>(url, cancellationToken);

            return (IReadOnlyList<TmdbMovieSummaryDto>)(response?.Results?.Select(MapSummary).ToList()
                ?? new List<TmdbMovieSummaryDto>());
        });
    }

    public async Task<IReadOnlyList<TmdbMovieSummaryDto>> DiscoverMoviesAsync(
        int? genreId,
        int? maxRuntimeMinutes,
        int? decadeStart,
        int page,
        CancellationToken cancellationToken)
    {
        var cacheKey = $"tmdb:discover:{genreId ?? -1}:{maxRuntimeMinutes ?? -1}:{decadeStart ?? -1}:{page}";

        return await GetOrCreateAsync(cacheKey, TimeSpan.FromMinutes(15), async () =>
        {
            var url = $"discover/movie?api_key={GetApiKey()}&page={Math.Max(page, 1)}&sort_by=popularity.desc";

            if (genreId.HasValue)
            {
                url += $"&with_genres={genreId.Value}";
            }

            if (maxRuntimeMinutes.HasValue)
            {
                url += $"&with_runtime.lte={maxRuntimeMinutes.Value}";
            }

            if (decadeStart.HasValue)
            {
                var startYear = decadeStart.Value;
                var endYear = startYear + 9;

                url += $"&primary_release_date.gte={startYear}-01-01";
                url += $"&primary_release_date.lte={endYear}-12-31";
            }

            var response = await GetFromJsonAsync<SearchMoviesResponse>(url, cancellationToken);

            return (IReadOnlyList<TmdbMovieSummaryDto>)(response?.Results?.Select(MapSummary).ToList()
                ?? new List<TmdbMovieSummaryDto>());
        });
    }

    private async Task<T?> GetFromJsonAsync<T>(string relativeUrl, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(relativeUrl, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return default;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions, cancellationToken);
    }

    private async Task<T> GetOrCreateAsync<T>(string cacheKey, TimeSpan duration, Func<Task<T>> factory)
    {
        if (_cache.TryGetValue(cacheKey, out T? cached) && cached is not null)
        {
            return cached;
        }

        var value = await factory();
        _cache.Set(cacheKey, value, duration);

        return value;
    }

    private string GetApiKey()
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException("TMDB ApiKey is missing. Configure Tmdb:ApiKey in appsettings.");
        }

        return _options.ApiKey;
    }

    private static TmdbMovieSummaryDto MapSummary(MovieSummaryResponse movie)
    {
        return new TmdbMovieSummaryDto
        {
            Id = movie.Id,
            Title = movie.Title ?? string.Empty,
            Overview = movie.Overview ?? string.Empty,
            PosterPath = movie.PosterPath,
            ReleaseDate = movie.ReleaseDate,
            VoteAverage = movie.VoteAverage
        };
    }

    private sealed class SearchMoviesResponse
    {
        public List<MovieSummaryResponse>? Results { get; set; }
    }

    private sealed class MovieSummaryResponse
    {
        public int Id { get; set; }
        public string? Title { get; set; }

        [JsonPropertyName("overview")]
        public string? Overview { get; set; }

        [JsonPropertyName("poster_path")]
        public string? PosterPath { get; set; }

        [JsonPropertyName("release_date")]
        public string? ReleaseDate { get; set; }

        [JsonPropertyName("vote_average")]
        public double VoteAverage { get; set; }
    }

    private sealed class MovieDetailResponse
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public string? Overview { get; set; }

        [JsonPropertyName("poster_path")]
        public string? PosterPath { get; set; }

        [JsonPropertyName("release_date")]
        public string? ReleaseDate { get; set; }
        public int Runtime { get; set; }

        [JsonPropertyName("vote_average")]
        public double VoteAverage { get; set; }
        public List<GenreResponse>? Genres { get; set; }
    }

    private sealed class GenreResponse
    {
        public string? Name { get; set; }
    }

    private sealed class CreditsResponse
    {
        public List<CastMemberResponse>? Cast { get; set; }
    }

    private sealed class CastMemberResponse
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Character { get; set; }
        public string? ProfilePath { get; set; }
        public int Order { get; set; }
    }
}
