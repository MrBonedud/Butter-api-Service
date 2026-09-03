using Core.Interfaces;
using Core.Entities;
using Microsoft.AspNetCore.SignalR;

namespace Butter_API.Hubs;

public class RoomHub : Hub
{
    private readonly IGuestSessionStore _guestSessionStore;
    private readonly IRoomRepository _roomRepository;

    public RoomHub(
        IGuestSessionStore guestSessionStore,
        IRoomRepository roomRepository)
    {
        _guestSessionStore = guestSessionStore;
        _roomRepository = roomRepository;
    }

    public async Task JoinRoom(
        string roomCode,
        Guid participantId,
        string sessionToken)
    {
        if (string.IsNullOrWhiteSpace(roomCode))
        {
            throw new HubException("Room code is required.");
        }

        var guest = _guestSessionStore.GetGuest(
            roomCode,
            participantId,
            sessionToken);

        if (guest is null)
        {
            throw new HubException("The participant session is invalid.");
        }

        await Groups.AddToGroupAsync(
            Context.ConnectionId,
            roomCode);

        Context.Items["roomCode"] = roomCode;
        Context.Items["participantId"] = participantId;

        var guests = _guestSessionStore
            .GetGuests(roomCode)
            .Select(item => new
            {
                item.ParticipantId,
                item.DisplayName,
                item.JoinedAtUtc
            });

        await Clients.Group(roomCode).SendAsync(
            "RoomPresence",
            guests);

    }

    public override async Task OnDisconnectedAsync(
        Exception? exception)
    {
        if (Context.Items.TryGetValue("roomCode", out var roomCode)
            && roomCode is string code
            && Context.Items.TryGetValue(
                "participantId",
                out var participantId)
            && participantId is Guid id)
        {
            _guestSessionStore.RemoveGuest(code, id);

            await Clients.OthersInGroup(code).SendAsync(
                "ParticipantLeft",
                id);

            if (_guestSessionStore.GetGuests(code).Count == 0)
            {
                var room = await _roomRepository.GetByCodeAsync(
                    code,
                    CancellationToken.None);

                if (room is not null && room.Status != RoomStatus.Closed)
                {
                    room.Status = RoomStatus.Closed;
                    room.ClosedAtUtc = DateTime.UtcNow;
                    room.LastActivityAtUtc = room.ClosedAtUtc.Value;
                    await _roomRepository.SaveChangesAsync(
                        CancellationToken.None);
                }
            }
        }

        await base.OnDisconnectedAsync(exception);
    }
}