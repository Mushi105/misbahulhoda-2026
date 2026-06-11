namespace Misbahuda.Application.Interfaces;

public interface IFcmService
{
    Task SendToTokenAsync(string fcmToken, string title, string body, CancellationToken cancellationToken = default);
    Task SendToTokensAsync(IEnumerable<string> fcmTokens, string title, string body, CancellationToken cancellationToken = default);
}
