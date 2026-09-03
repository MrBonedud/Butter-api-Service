using Core.DTOs;

namespace Core.Interfaces;

public interface ISwipeService
{
    Task<SwipeResponse> RecordAsync(
        string roomCode,
        RecordSwipeRequest request,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<TmdbMovieSummaryDto>> GetCandidatesAsync(
string roomCode,
Guid participantId,
string sessionToken,
int count,
CancellationToken cancellationToken);
}