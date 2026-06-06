using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Misbahuda.API.Hubs;

[Authorize]
public class TrackingHub : Hub
{
    public async Task JoinKarwanGroup(string karwanId) =>
        await Groups.AddToGroupAsync(Context.ConnectionId, $"karwan-{karwanId}");

    public async Task LeaveKarwanGroup(string karwanId) =>
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"karwan-{karwanId}");

    public async Task JoinBusGroup(string busId) =>
        await Groups.AddToGroupAsync(Context.ConnectionId, $"bus-{busId}");

    public override async Task OnConnectedAsync()
    {
        await Clients.Caller.SendAsync("Connected", Context.ConnectionId);
        await base.OnConnectedAsync();
    }
}
