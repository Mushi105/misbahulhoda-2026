using Microsoft.AspNetCore.SignalR;
using Misbahuda.API.Hubs;
using Misbahuda.Application.Interfaces;

namespace Misbahuda.API.Services;

public class SignalRPusher(
    IHubContext<NotificationHub> notifHub,
    IHubContext<TrackingHub> trackingHub) : IRealtimePusher
{
    public async Task PushToUserAsync(string userId, string eventName, object payload)
    {
        await notifHub.Clients.Group($"user-{userId}").SendAsync(eventName, payload);
    }

    public async Task PushToAllAsync(string eventName, object payload)
    {
        await notifHub.Clients.All.SendAsync(eventName, payload);
    }
}
