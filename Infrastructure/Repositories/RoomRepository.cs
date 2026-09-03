using Core.Entities;
using Core.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class RoomRepository : IRoomRepository
{
    private readonly AppDbContext _context;

    public RoomRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<bool> CodeExistsAsync(
        string code,
        CancellationToken cancellationToken)
    {
        return await _context.Rooms.AnyAsync(
            room => room.Code == code,
            cancellationToken);
    }

    public async Task AddAsync(
        Room room,
        CancellationToken cancellationToken)
    {
        await _context.Rooms.AddAsync(room, cancellationToken);
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<Room?> GetByCodeAsync(
        string code,
        CancellationToken cancellationToken)
    {
        return await _context.Rooms
            .AsTracking()
            .FirstOrDefaultAsync(
                room => room.Code == code,
                cancellationToken);
    }

}