using Core.Models;

namespace Core.Interfaces;

public interface IGuestSessionStore
{
    GuestSession AddGuest(
        string roomCode,
        string displayName);

    IReadOnlyList<GuestSession> GetGuests(
        string roomCode);

    void RemoveRoom(
        string roomCode);

    bool RemoveGuest(
        string roomCode,
        Guid participantId);

    GuestSession? GetGuest(
string roomCode,
Guid participantId,
string sessionToken);
}