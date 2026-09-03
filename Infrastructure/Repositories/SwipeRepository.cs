using Core.Entities;
using Core.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class SwipeRepository : ISwipeRepository
{
    private readonly AppDbContext _context;

    public SwipeRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<bool> ExistsAsync(
        Guid roomId,
        Guid participantId,
        int tmdbMovieId,
        CancellationToken cancellationToken)
    {
        return await _context.Swipes.AnyAsync(
            swipe =>
                swipe.RoomId == roomId &&
                swipe.ParticipantId == participantId &&
                swipe.TmdbMovieId == tmdbMovieId,
            cancellationToken);
    }

    public async Task AddAsync(
        Swipe swipe,
        CancellationToken cancellationToken)
    {
        await _context.Swipes.AddAsync(
            swipe,
            cancellationToken);
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<int>> GetSwipedMovieIdsAsync(
    Guid roomId,
    Guid participantId,
    CancellationToken cancellationToken)
    {
        return await _context.Swipes
            .Where(swipe =>
                swipe.RoomId == roomId &&
                swipe.ParticipantId == participantId)
            .Select(swipe => swipe.TmdbMovieId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<int>> GetSwipedMovieIdsAsync(
        Guid roomId,
        CancellationToken cancellationToken)
    {
        return await _context.Swipes
            .Where(swipe => swipe.RoomId == roomId)
            .Select(swipe => swipe.TmdbMovieId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> GetSwipedParticipantIdsAsync(
        Guid roomId,
        int tmdbMovieId,
        CancellationToken cancellationToken)
    {
        return await _context.Swipes
            .Where(swipe =>
                swipe.RoomId == roomId &&
                swipe.TmdbMovieId == tmdbMovieId)
            .Select(swipe => swipe.ParticipantId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SwipeDirection>> GetSwipeDirectionsAsync(
        Guid roomId,
        int tmdbMovieId,
        CancellationToken cancellationToken)
    {
        return await _context.Swipes
            .Where(swipe =>
                swipe.RoomId == roomId &&
                swipe.TmdbMovieId == tmdbMovieId)
            .Select(swipe => swipe.Direction)
            .ToListAsync(cancellationToken);
    }
}