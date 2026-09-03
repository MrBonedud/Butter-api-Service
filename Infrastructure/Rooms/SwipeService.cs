using Core.DTOs;
using Core.Entities;
using Core.Interfaces;
using System.Collections.Concurrent;

namespace Infrastructure.Rooms;

public class SwipeService : ISwipeService
{
    private static readonly ConcurrentDictionary<Guid, SemaphoreSlim> RoomLocks = new();

    private readonly IRoomRepository _roomRepository;
    private readonly ISwipeRepository _swipeRepository;
    private readonly IGuestSessionStore _guestSessionStore;
    private readonly ITmdbService _tmdbService;
    public SwipeService(
        IRoomRepository roomRepository,
        ISwipeRepository swipeRepository,
        IGuestSessionStore guestSessionStore,
        ITmdbService tmdbService)
    {
        _roomRepository = roomRepository;
        _swipeRepository = swipeRepository;
        _guestSessionStore = guestSessionStore;
        _tmdbService = tmdbService;
    }

    public async Task<SwipeResponse> RecordAsync(
        string roomCode,
        RecordSwipeRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(roomCode))
        {
            throw new ArgumentException(
                "Room code is required.",
                nameof(roomCode));
        }

        if (request.ParticipantId == Guid.Empty)
        {
            throw new ArgumentException(
                "Participant ID is required.",
                nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.SessionToken))
        {
            throw new ArgumentException(
                "Session token is required.",
                nameof(request));
        }

        if (request.TmdbMovieId <= 0)
        {
            throw new ArgumentException(
                "A valid TMDB movie ID is required.",
                nameof(request));
        }

        if (!Enum.IsDefined(request.Direction))
        {
            throw new ArgumentException(
                "Swipe direction is invalid.",
                nameof(request));
        }

        var room = await _roomRepository.GetByCodeAsync(
            roomCode,
            cancellationToken);

        if (room is null)
        {
            throw new KeyNotFoundException(
                $"Room '{roomCode}' was not found.");
        }

        if (room.Status != RoomStatus.Swiping)
        {
            throw new InvalidOperationException(
                "This room is not currently accepting swipes.");
        }

        var guest = _guestSessionStore.GetGuest(
            room.Code,
            request.ParticipantId,
            request.SessionToken);

        if (guest is null)
        {
            throw new UnauthorizedAccessException(
                "The participant session is invalid.");
        }

        if (room.CurrentMovieId != request.TmdbMovieId)
        {
            throw new InvalidOperationException(
                "This movie is no longer the current room choice.");
        }

        var alreadyExists = await _swipeRepository.ExistsAsync(
            room.Id,
            guest.ParticipantId,
            request.TmdbMovieId,
            cancellationToken);

        if (alreadyExists)
        {
            throw new InvalidOperationException(
                "This participant has already swiped on that movie.");
        }

        var swipe = new Swipe
        {
            Id = Guid.NewGuid(),
            RoomId = room.Id,
            ParticipantId = guest.ParticipantId,
            GuestName = guest.DisplayName,
            TmdbMovieId = request.TmdbMovieId,
            Direction = request.Direction,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _swipeRepository.AddAsync(
            swipe,
            cancellationToken);

        room.LastActivityAtUtc = DateTime.UtcNow;

        await _swipeRepository.SaveChangesAsync(
            cancellationToken);

        var participantIds = _guestSessionStore
            .GetGuests(room.Code)
            .Select(item => item.ParticipantId)
            .ToHashSet();

        var swipedParticipantIds = (
            await _swipeRepository.GetSwipedParticipantIdsAsync(
                room.Id,
                request.TmdbMovieId,
                cancellationToken))
            .ToHashSet();

        var swipeDirections = await _swipeRepository.GetSwipeDirectionsAsync(
            room.Id,
            request.TmdbMovieId,
            cancellationToken);

        var allParticipantsHaveVoted = participantIds.Count > 0
            && participantIds.IsSubsetOf(swipedParticipantIds);

        var isMatch = allParticipantsHaveVoted
            && swipeDirections.All(direction => direction == SwipeDirection.Right);

        if (isMatch)
        {
            room.Status = RoomStatus.Matched;
        }
        else if (allParticipantsHaveVoted)
        {
            room.CurrentMovieId = null;
        }

        if (isMatch || allParticipantsHaveVoted)
        {
            await _roomRepository.SaveChangesAsync(cancellationToken);
        }

        return new SwipeResponse
        {
            Id = swipe.Id,
            ParticipantId = swipe.ParticipantId,
            TmdbMovieId = swipe.TmdbMovieId,
            Direction = swipe.Direction,
            CreatedAtUtc = swipe.CreatedAtUtc,
            IsMatch = isMatch
        };
    }

    public async Task<IReadOnlyList<TmdbMovieSummaryDto>> GetCandidatesAsync(
    string roomCode,
    Guid participantId,
    string sessionToken,
    int count,
    CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(roomCode))
        {
            throw new ArgumentException(
                "Room code is required.",
                nameof(roomCode));
        }

        if (participantId == Guid.Empty)
        {
            throw new ArgumentException(
                "Participant ID is required.",
                nameof(participantId));
        }

        if (string.IsNullOrWhiteSpace(sessionToken))
        {
            throw new ArgumentException(
                "Session token is required.",
                nameof(sessionToken));
        }

        count = Math.Clamp(count, 1, 20);

        var room = await _roomRepository.GetByCodeAsync(
            roomCode,
            cancellationToken);

        if (room is null)
        {
            throw new KeyNotFoundException(
                $"Room '{roomCode}' was not found.");
        }

        if (room.Status != RoomStatus.Swiping)
        {
            throw new InvalidOperationException(
                "This room is not currently accepting swipes.");
        }

        var guest = _guestSessionStore.GetGuest(
            room.Code,
            participantId,
            sessionToken);

        if (guest is null)
        {
            throw new UnauthorizedAccessException(
                "The participant session is invalid.");
        }

        var roomLock = RoomLocks.GetOrAdd(
            room.Id,
            _ => new SemaphoreSlim(1, 1));

        await roomLock.WaitAsync(cancellationToken);

        try
        {
            room = await _roomRepository.GetByCodeAsync(
                roomCode,
                cancellationToken);

            if (room is null)
            {
                throw new KeyNotFoundException(
                    $"Room '{roomCode}' was not found.");
            }

            var swipedMovieIds = (
                await _swipeRepository.GetSwipedMovieIdsAsync(
                    room.Id,
                    cancellationToken))
                .ToHashSet();

            var movies = await _tmdbService.DiscoverMoviesAsync(
                room.GenreId,
                room.MaxRuntimeMinutes,
                room.DecadeStart,
                Random.Shared.Next(1, 6),
                cancellationToken);

            if (room.CurrentMovieId is null)
            {
                var nextMovie = movies.FirstOrDefault(
                    movie => !swipedMovieIds.Contains(movie.Id));

                if (nextMovie is null)
                {
                    return Array.Empty<TmdbMovieSummaryDto>();
                }

                room.CurrentMovieId = nextMovie.Id;
                room.LastActivityAtUtc = DateTime.UtcNow;
                await _roomRepository.SaveChangesAsync(cancellationToken);
            }

            var candidates = movies
                .Where(movie =>
                    movie.Id == room.CurrentMovieId ||
                    !swipedMovieIds.Contains(movie.Id))
                .Take(count)
                .ToList();

            var currentMovie = candidates.FirstOrDefault(
                movie => movie.Id == room.CurrentMovieId);

            if (currentMovie is null)
            {
                var detail = await _tmdbService.GetMovieDetailAsync(
                    room.CurrentMovieId.Value,
                    cancellationToken);

                if (detail is not null)
                {
                    candidates.Insert(0, new TmdbMovieSummaryDto
                    {
                        Id = detail.Id,
                        Title = detail.Title,
                        Overview = detail.Overview,
                        PosterPath = detail.PosterPath,
                        ReleaseDate = detail.ReleaseDate,
                        VoteAverage = detail.VoteAverage,
                        Genres = detail.Genres,
                        Runtime = detail.Runtime > 0 ? detail.Runtime : null
                    });
                }
            }

            var detailedCandidates = await Task.WhenAll(
                candidates.Select(async movie =>
                {
                    var detail = await _tmdbService.GetMovieDetailAsync(
                        movie.Id,
                        cancellationToken);

                    return new TmdbMovieSummaryDto
                    {
                        Id = movie.Id,
                        Title = movie.Title,
                        Overview = movie.Overview,
                        PosterPath = movie.PosterPath,
                        ReleaseDate = movie.ReleaseDate,
                        VoteAverage = movie.VoteAverage,
                        Genres = detail?.Genres ?? new List<string>(),
                        Runtime = detail?.Runtime is > 0 ? detail.Runtime : null
                    };
                }));

            room.LastActivityAtUtc = DateTime.UtcNow;
            await _roomRepository.SaveChangesAsync(cancellationToken);

            return detailedCandidates;
        }
        finally
        {
            roomLock.Release();
        }
    }
}