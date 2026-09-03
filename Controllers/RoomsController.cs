using System.Security.Claims;
using Core.DTOs;
using Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Butter_API.Hubs;

namespace Butter_API.Controllers;

[ApiController]
[Route("api/rooms")]
[Authorize]
public class RoomsController : ControllerBase
{
    private readonly IRoomService _roomService;
    private readonly IHubContext<RoomHub> _hubContext;

    public RoomsController(
        IRoomService roomService,
        IHubContext<RoomHub> hubContext)
    {
        _roomService = roomService;
        _hubContext = hubContext;
    }

    [HttpPost]
    public async Task<ActionResult<CreateRoomResponse>> Create(
        [FromBody] RoomSettingsRequest? settings,
        CancellationToken cancellationToken)
    {
        var userIdValue =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdValue, out var creatorId))
        {
            return Unauthorized();
        }

        var displayName = User.FindFirstValue("displayName");

        if (string.IsNullOrWhiteSpace(displayName))
        {
            return Unauthorized();
        }

        try
        {
            var room = await _roomService.CreateAsync(
                creatorId,
                displayName,
                settings,
                cancellationToken);

            return StatusCode(
                StatusCodes.Status201Created,
                room);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpPatch("{code}/settings")]
    public async Task<ActionResult<RoomResponse>> UpdateSettings(
        string code,
        [FromBody] RoomSettingsRequest request,
        CancellationToken cancellationToken)
    {
        var userIdValue =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdValue, out var creatorId))
        {
            return Unauthorized();
        }

        try
        {
            var room = await _roomService.UpdateSettingsAsync(
                code,
                creatorId,
                request,
                cancellationToken);

            return Ok(room);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
    }

    [HttpGet("{code}")]
    [AllowAnonymous]
    public async Task<ActionResult<RoomDetailsResponse>> GetDetails(
       string code,
       CancellationToken cancellationToken)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        Guid? requesterId = null;
        if (Guid.TryParse(userIdValue, out var parsedUserId))
        {
            requesterId = parsedUserId;
        }

        try
        {
            var room = await _roomService.GetDetailsAsync(
                code,
                requesterId,
                cancellationToken);

            return Ok(room);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
    }

    [HttpPost("{code}/start-swiping")]
    public async Task<ActionResult<RoomResponse>> StartSwiping(
        string code,
        CancellationToken cancellationToken)
    {
        var userIdValue =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdValue, out var creatorId))
        {
            return Unauthorized();
        }

        try
        {
            var room = await _roomService.StartSwipingAsync(
                code,
                creatorId,
                cancellationToken);

            await _hubContext.Clients
                .Group(code)
                .SendAsync("SwipingStarted", cancellationToken);

            return Ok(room);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new { message = exception.Message });
        }
    }

    [HttpPost("{code}/join")]
    [AllowAnonymous]
    public async Task<ActionResult<JoinRoomResponse>> Join(
        string code,
        JoinRoomRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _roomService.JoinAsync(
                code,
                request,
                cancellationToken);

            await _hubContext.Clients
                .Group(code)
                .SendAsync(
                    "ParticipantJoined",
                    new
                    {
                        response.ParticipantId,
                        response.DisplayName,
                        response.JoinedAtUtc
                    },
                    cancellationToken);

            return Ok(response);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new { message = exception.Message });
        }
    }

    [HttpGet("{code}/candidates")]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<RoomMovieCandidateResponse>>> GetMovieCandidates(
    string code,
    CancellationToken cancellationToken)
    {
        try
        {
            var candidates = await _roomService.GetMovieCandidatesAsync(
                code,
                cancellationToken);

            return Ok(candidates);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new { message = exception.Message });
        }
    }
}