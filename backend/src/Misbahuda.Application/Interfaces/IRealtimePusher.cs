namespace Misbahuda.Application.Interfaces;

public interface IRealtimePusher
{
    Task PushToUserAsync(string userId, string eventName, object payload);
    Task PushToAllAsync(string eventName, object payload);
}
