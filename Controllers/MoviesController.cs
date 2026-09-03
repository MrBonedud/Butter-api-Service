using Core.DTOs;
using Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Butter_API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MoviesController : ControllerBase
{
    private readonly ITmdbService _tmdbService;

    public MoviesController(ITmdbService tmdbService)
    {
        _tmdbService = tmdbService;
    }

    [HttpGet("search")]
    public async Task<ActionResult<IReadOnlyList<TmdbMovieSummaryDto>>> Search(
        [FromQuery] string query,
        [FromQuery] int page = 1,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return BadRequest("Query is required.");
        }

        var result = await _tmdbService.SearchMoviesAsync(query, Math.Max(page, 1), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{movieId:int}")]
    public async Task<ActionResult<TmdbMovieDetailDto>> GetDetail(int movieId, CancellationToken cancellationToken)
    {
        var result = await _tmdbService.GetMovieDetailAsync(movieId, cancellationToken);

        if (result is null)
        {
            return NotFound();
        }

        return Ok(result);
    }

    [HttpGet("{movieId:int}/cast")]
    public async Task<ActionResult<IReadOnlyList<TmdbCastMemberDto>>> GetCast(int movieId, CancellationToken cancellationToken)
    {
        var result = await _tmdbService.GetMovieCastAsync(movieId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{movieId:int}/similar")]
    public async Task<ActionResult<IReadOnlyList<TmdbMovieSummaryDto>>> GetSimilar(
        int movieId,
        [FromQuery] int page = 1,
        CancellationToken cancellationToken = default)
    {
        var result = await _tmdbService.GetSimilarMoviesAsync(movieId, Math.Max(page, 1), cancellationToken);
        return Ok(result);
    }
}
