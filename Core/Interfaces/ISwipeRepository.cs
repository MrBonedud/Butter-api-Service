using Core.Entities;

namespace Core.Interfaces;

public interface ISwipeRepository
{
    Task<bool> ExistsAsync(
        Guid roomId,
        Guid participantId,
        int tmdbMovieId,
        CancellationToken cancellationToken);

    Task AddAsync(
        Swipe swipe,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(
        CancellationToken cancellationToken);

    Task<IReadOnlyList<int>> GetSwipedMovieIdsAsync(
Guid roomId,
Guid participantId,
CancellationToken cancellationToken);

    Task<IReadOnlyList<int>> GetSwipedMovieIdsAsync(
        Guid roomId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Guid>> GetSwipedParticipantIdsAsync(
        Guid roomId,
        int tmdbMovieId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<SwipeDirection>> GetSwipeDirectionsAsync(
        Guid roomId,
        int tmdbMovieId,
        CancellationToken cancellationToken);

}