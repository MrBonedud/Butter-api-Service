using Core.DTOs;
using Core.Entities;
using Core.Interfaces;

namespace Infrastructure.Rooms;

public class RoomService : IRoomService
{
    private const int MaximumCodeAttempts = 10;
    private static readonly TimeSpan RoomInactivityTimeout = TimeSpan.FromMinutes(30);

    private readonly IRoomRepository _roomRepository;
    private readonly IRoomCodeGenerator _codeGenerator;
    private readonly IGuestSessionStore _guestSessionStore;

    private readonly ITmdbService _tmdbService;

    public RoomService(
        IRoomRepository roomRepository,
        IRoomCodeGenerator codeGenerator,
        IGuestSessionStore guestSessionStore,
        ITmdbService tmdbService)
    {
        _roomRepository = roomRepository;
        _codeGenerator = codeGenerator;
        _guestSessionStore = guestSessionStore;
        _tmdbService = tmdbService;
    }

    public async Task<CreateRoomResponse> CreateAsync(
        Guid creatorId,
        string creatorDisplayName,
        RoomSettingsRequest? settings,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(creatorDisplayName))
        {
            throw new ArgumentException(
                "Creator display name is required.",
                nameof(creatorDisplayName));
        }

        var code = await GenerateUniqueCodeAsync(cancellationToken);
        var now = DateTime.UtcNow;

        var room = new Room
        {
            Id = Guid.NewGuid(),
            Code = code,
            CreatorId = creatorId,
            GenreId = NormalizeGenreId(settings?.GenreId),
            MaxRuntimeMinutes = NormalizeMaxRuntimeMinutes(settings?.MaxRuntimeMinutes),
            DecadeStart = NormalizeDecadeStart(settings?.DecadeStart),
            Status = RoomStatus.Waiting,
            CreatedAtUtc = now,
            LastActivityAtUtc = now
        };

        await _roomRepository.AddAsync(room, cancellationToken);
        await _roomRepository.SaveChangesAsync(cancellationToken);

        var creatorGuest = _guestSessionStore.AddGuest(
            room.Code,
            creatorDisplayName.Trim());

        return new CreateRoomResponse
        {
            Room = Map(room),
            ParticipantId = creatorGuest.ParticipantId,
            DisplayName = creatorGuest.DisplayName,
            SessionToken = creatorGuest.SessionToken,
            JoinedAtUtc = creatorGuest.JoinedAtUtc
        };
    }

    public async Task<RoomResponse> UpdateSettingsAsync(
        string code,
        Guid creatorId,
        RoomSettingsRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Room code is required.", nameof(code));
        }

        var room = await _roomRepository.GetByCodeAsync(
            code,
            cancellationToken);


        if (room is null)
        {
            throw new KeyNotFoundException($"Room '{code}' was not found.");
        }

        await EnsureRoomIsActiveAsync(room, code, cancellationToken);

        if (room.CreatorId != creatorId)
        {
            throw new UnauthorizedAccessException(
                "Only the room creator can update the room settings.");
        }

        room.GenreId = NormalizeGenreId(request.GenreId);
        room.MaxRuntimeMinutes = NormalizeMaxRuntimeMinutes(request.MaxRuntimeMinutes);
        room.DecadeStart = NormalizeDecadeStart(request.DecadeStart);
        room.LastActivityAtUtc = DateTime.UtcNow;

        await _roomRepository.SaveChangesAsync(cancellationToken);

        return Map(room);
    }

    public async Task<RoomDetailsResponse> GetDetailsAsync(
        string code,
        Guid? requesterId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Room code is required.", nameof(code));
        }

        var room = await _roomRepository.GetByCodeAsync(
            code,
            cancellationToken);


        if (room is null)
        {
            throw new KeyNotFoundException($"Room '{code}' was not found.");
        }

        await EnsureRoomIsActiveAsync(room, code, cancellationToken);

        var guests = _guestSessionStore
            .GetGuests(room.Code)
            .Select(guest => new RoomGuestResponse
            {
                ParticipantId = guest.ParticipantId,
                DisplayName = guest.DisplayName,
                JoinedAtUtc = guest.JoinedAtUtc
            })
            .OrderBy(guest => guest.JoinedAtUtc)
            .ToList();

        room.LastActivityAtUtc = DateTime.UtcNow;
        await _roomRepository.SaveChangesAsync(cancellationToken);

        return new RoomDetailsResponse
        {
            Id = room.Id,
            Code = room.Code,
            CreatorId = room.CreatorId,
            GenreId = room.GenreId,
            MaxRuntimeMinutes = room.MaxRuntimeMinutes,
            DecadeStart = room.DecadeStart,
            Status = room.Status.ToString(),
            CreatedAtUtc = room.CreatedAtUtc,
            LastActivityAtUtc = room.LastActivityAtUtc,
            Guests = guests
        };
    }

    public async Task<RoomResponse> StartSwipingAsync(
        string code,
        Guid creatorId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Room code is required.", nameof(code));
        }

        var room = await _roomRepository.GetByCodeAsync(
            code,
            cancellationToken);

        if (room is null)
        {
            throw new KeyNotFoundException($"Room '{code}' was not found.");
        }

        await EnsureRoomIsActiveAsync(room, code, cancellationToken);

        if (room.CreatorId != creatorId)
        {
            throw new UnauthorizedAccessException(
                "Only the room creator can start swiping.");
        }

        room.Status = RoomStatus.Swiping;
        room.LastActivityAtUtc = DateTime.UtcNow;

        await _roomRepository.SaveChangesAsync(cancellationToken);

        return Map(room);
    }

    public async Task<JoinRoomResponse> JoinAsync(
        string code,
        JoinRoomRequest request,
        CancellationToken cancellationToken)
    {


        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Room code is required.", nameof(code));
        }

        if (string.IsNullOrWhiteSpace(request.DisplayName))
        {
            throw new ArgumentException("Display name is required.", nameof(request));
        }

        var room = await _roomRepository.GetByCodeAsync(
            code,
            cancellationToken);

        if (room is null)
        {
            throw new KeyNotFoundException($"Room '{code}' was not found.");
        }

        await EnsureRoomIsActiveAsync(room, code, cancellationToken);

        var guest = _guestSessionStore.AddGuest(
            room.Code,
            request.DisplayName.Trim());

        room.LastActivityAtUtc = DateTime.UtcNow;
        await _roomRepository.SaveChangesAsync(cancellationToken);

        return new JoinRoomResponse
        {
            ParticipantId = guest.ParticipantId,
            RoomCode = guest.RoomCode,
            DisplayName = guest.DisplayName,
            SessionToken = guest.SessionToken,
            JoinedAtUtc = guest.JoinedAtUtc
        };
    }

    private async Task<string> GenerateUniqueCodeAsync(
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < MaximumCodeAttempts; attempt++)
        {
            var code = _codeGenerator.Generate();

            if (!await _roomRepository.CodeExistsAsync(
                    code,
                    cancellationToken))
            {
                return code;
            }
        }

        throw new InvalidOperationException(
            "Unable to generate a unique room code.");
    }

    private static int? NormalizeGenreId(int? genreId)
    {
        if (genreId is null)
        {
            return null;
        }

        if (genreId <= 0)
        {
            throw new ArgumentException(
                "GenreId must be greater than zero when provided.",
                nameof(genreId));
        }

        return genreId;
    }

    private static int? NormalizeMaxRuntimeMinutes(int? maxRuntimeMinutes)
    {
        if (maxRuntimeMinutes is null)
        {
            return null;
        }

        if (maxRuntimeMinutes <= 0 || maxRuntimeMinutes > 600)
        {
            throw new ArgumentException(
                "Max runtime must be between 1 and 600 minutes.",
                nameof(maxRuntimeMinutes));
        }

        return maxRuntimeMinutes;
    }

    private static int? NormalizeDecadeStart(int? decadeStart)
    {
        if (decadeStart is null)
        {
            return null;
        }

        if (decadeStart < 1900 || decadeStart > 2100 || decadeStart % 10 != 0)
        {
            throw new ArgumentException(
                "Decade filter must be a valid 10-year start year, for example 1990.",
                nameof(decadeStart));
        }

        return decadeStart;
    }

    private static RoomResponse Map(Room room)
    {
        return new RoomResponse
        {
            Id = room.Id,
            Code = room.Code,
            CreatorId = room.CreatorId,
            GenreId = room.GenreId,
            MaxRuntimeMinutes = room.MaxRuntimeMinutes,
            DecadeStart = room.DecadeStart,
            Status = room.Status.ToString(),
            CreatedAtUtc = room.CreatedAtUtc
        };
    }

    private static bool IsExpired(Room room, TimeSpan inactivityWindow)
    {
        return room.LastActivityAtUtc < DateTime.UtcNow.Subtract(inactivityWindow);
    }

    private async Task EnsureRoomIsActiveAsync(Room room, string code, CancellationToken cancellationToken)
    {
        if (room.Status == RoomStatus.Closed)
        {
            _guestSessionStore.RemoveRoom(room.Code);
            throw new InvalidOperationException("This room is closed.");
        }

        if (IsExpired(room, RoomInactivityTimeout))
        {
            await CloseRoomAsync(room, cancellationToken);
            throw new InvalidOperationException("This room has expired due to inactivity.");
        }
    }

    private async Task CloseRoomAsync(Room room, CancellationToken cancellationToken)
    {
        room.Status = RoomStatus.Closed;
        room.ClosedAtUtc = DateTime.UtcNow;
        room.LastActivityAtUtc = room.ClosedAtUtc.Value;

        _guestSessionStore.RemoveRoom(room.Code);

        await _roomRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RoomMovieCandidateResponse>> GetMovieCandidatesAsync(
    string code,
    CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Room code is required.", nameof(code));
        }

        var room = await _roomRepository.GetByCodeAsync(code, cancellationToken);

        if (room is null)
        {
            throw new KeyNotFoundException($"Room '{code}' was not found.");
        }

        await EnsureRoomIsActiveAsync(room, code, cancellationToken);

        var movies = await _tmdbService.DiscoverMoviesAsync(
            room.GenreId,
            room.MaxRuntimeMinutes,
            room.DecadeStart,
            1,
            cancellationToken);

        return movies
            .Select(movie => new RoomMovieCandidateResponse
            {
                MovieId = movie.Id,
                Title = movie.Title,
                Overview = movie.Overview,
                PosterUrl = movie.PosterPath ?? string.Empty,
                ReleaseYear = movie.ReleaseDate is null
                    ? 0
                    : DateOnly.FromDateTime(DateTime.Parse(movie.ReleaseDate)).Year,
                VoteAverage = movie.VoteAverage
            })
            .ToList();
    }
}