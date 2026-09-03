using Core.Entities;

namespace Core.Interfaces;

public interface IRoomRepository
{
    Task<bool> CodeExistsAsync(
        string code,
        CancellationToken cancellationToken);

    Task<Room?> GetByCodeAsync(
        string code,
        CancellationToken cancellationToken);

    Task AddAsync(
        Room room,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(
        CancellationToken cancellationToken);
}