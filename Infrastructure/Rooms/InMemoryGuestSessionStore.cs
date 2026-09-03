using System.Collections.Concurrent;
using System.Security.Cryptography;
using Core.Interfaces;
using Core.Models;

namespace Infrastructure.Rooms;

public class InMemoryGuestSessionStore : IGuestSessionStore
{
    private readonly ConcurrentDictionary<string, List<GuestSession>>
        _guestsByRoom = new(StringComparer.OrdinalIgnoreCase);

    public GuestSession AddGuest(
        string roomCode,
        string displayName)
    {
        var guests = _guestsByRoom.GetOrAdd(
            roomCode,
            _ => new List<GuestSession>());

        lock (guests)
        {
            var nameAlreadyExists = guests.Any(
                guest => string.Equals(
                    guest.DisplayName,
                    displayName,
                    StringComparison.OrdinalIgnoreCase));

            if (nameAlreadyExists)
            {
                throw new InvalidOperationException(
                    "That display name is already in use in this room.");
            }

            var guest = new GuestSession
            {
                ParticipantId = Guid.NewGuid(),
                RoomCode = roomCode,
                DisplayName = displayName,
                SessionToken = Convert.ToHexString(
                    RandomNumberGenerator.GetBytes(32)),
                JoinedAtUtc = DateTime.UtcNow
            };

            guests.Add(guest);

            return guest;
        }
    }

    public IReadOnlyList<GuestSession> GetGuests(
        string roomCode)
    {
        if (string.IsNullOrWhiteSpace(roomCode))
        {
            return Array.Empty<GuestSession>();
        }

        if (_guestsByRoom.TryGetValue(
                roomCode,
                out var guests))
        {
            lock (guests)
            {
                return guests
                    .OrderBy(guest => guest.JoinedAtUtc)
                    .ToList();
            }
        }

        return Array.Empty<GuestSession>();
    }

    public void RemoveRoom(
        string roomCode)
    {
        if (string.IsNullOrWhiteSpace(roomCode))
        {
            return;
        }

        _guestsByRoom.TryRemove(
            roomCode,
            out _);
    }

    public bool RemoveGuest(
        string roomCode,
        Guid participantId)
    {
        if (!_guestsByRoom.TryGetValue(roomCode, out var guests))
        {
            return false;
        }

        lock (guests)
        {
            var removed = guests.RemoveAll(
                guest => guest.ParticipantId == participantId) > 0;

            if (guests.Count == 0)
            {
                _guestsByRoom.TryRemove(roomCode, out _);
            }

            return removed;
        }
    }

    public GuestSession? GetGuest(
    string roomCode,
    Guid participantId,
    string sessionToken)
    {
        var guests = GetGuests(roomCode);

        return guests.FirstOrDefault(guest =>
            guest.ParticipantId == participantId &&
            string.Equals(
                guest.SessionToken,
                sessionToken,
                StringComparison.Ordinal));
    }
}