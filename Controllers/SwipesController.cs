using Core.DTOs;
using Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Butter_API.Hubs;


namespace Butter_API.Controllers;

[ApiController]
[Route("api/rooms/{code}/swipes")]
[AllowAnonymous]
public class SwipesController : ControllerBase
{
    private readonly ISwipeService _swipeService;
    private readonly IHubContext<RoomHub> _hubContext;

    public SwipesController(
        ISwipeService swipeService,
        IHubContext<RoomHub> hubContext)
    {
        _swipeService = swipeService;
        _hubContext = hubContext;
    }

    [HttpPost]
    public async Task<ActionResult<SwipeResponse>> Record(
        string code,
        [FromBody] RecordSwipeRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _swipeService.RecordAsync(
                code,
                request,
                cancellationToken);

            await _hubContext.Clients
                .Group(code)
                .SendAsync(
                    "SwipeRecorded",
                    new
                    {
                        participantId = request.ParticipantId
                    },
                    cancellationToken);

            if (response.IsMatch)
            {
                await _hubContext.Clients
                    .Group(code)
                    .SendAsync(
                        "MovieMatched",
                        new
                        {
                            tmdbMovieId = response.TmdbMovieId
                        },
                        cancellationToken);
            }

            return Ok(response);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new
            {
                message = exception.Message
            });
        }
        catch (UnauthorizedAccessException exception)
        {
            return Unauthorized(new
            {
                message = exception.Message
            });
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new
            {
                message = exception.Message
            });
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new
            {
                message = exception.Message
            });
        }
    }

    [HttpGet("candidates")]
    public async Task<ActionResult<IReadOnlyList<TmdbMovieSummaryDto>>> GetCandidates(
    string code,
    [FromQuery] Guid participantId,
    [FromQuery] string sessionToken,
    [FromQuery] int count = 5,
    CancellationToken cancellationToken = default)
    {
        try
        {
            var candidates = await _swipeService.GetCandidatesAsync(
                code,
                participantId,
                sessionToken,
                count,
                cancellationToken);

            return Ok(candidates);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new
            {
                message = exception.Message
            });
        }
        catch (UnauthorizedAccessException exception)
        {
            return Unauthorized(new
            {
                message = exception.Message
            });
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new
            {
                message = exception.Message
            });
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new
            {
                message = exception.Message
            });
        }
    }
}