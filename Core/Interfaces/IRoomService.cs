using Core.DTOs;

namespace Core.Interfaces;

public interface IRoomService
{
    Task<CreateRoomResponse> CreateAsync(
        Guid creatorId,
        string creatorDisplayName,
        RoomSettingsRequest? settings,
        CancellationToken cancellationToken);

    Task<RoomResponse> UpdateSettingsAsync(
        string code,
        Guid creatorId,
        RoomSettingsRequest request,
        CancellationToken cancellationToken);

    Task<RoomDetailsResponse> GetDetailsAsync(
        string code,
        Guid? requesterId,
        CancellationToken cancellationToken);

    Task<RoomResponse> StartSwipingAsync(
        string code,
        Guid creatorId,
        CancellationToken cancellationToken);

    Task<JoinRoomResponse> JoinAsync(
        string code,
        JoinRoomRequest request,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<RoomMovieCandidateResponse>> GetMovieCandidatesAsync(
        string code,
        CancellationToken cancellationToken);
}