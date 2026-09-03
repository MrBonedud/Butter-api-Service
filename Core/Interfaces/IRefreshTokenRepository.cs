namespace Core.Interfaces;

using Core.Entities;


public interface IRefreshTokenRepository
{
    Task AddAsync(
        RefreshToken refreshToken,
        CancellationToken cancellationToken);

    Task<RefreshToken?> GetByTokenAsync(
        string token,
        CancellationToken cancellationToken);

    Task UpdateAsync(
        RefreshToken refreshToken,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(
        CancellationToken cancellationToken);
}
