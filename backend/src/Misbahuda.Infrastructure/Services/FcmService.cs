using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Misbahuda.Application.Interfaces;

namespace Misbahuda.Infrastructure.Services;

public class FcmService : IFcmService
{
    private readonly ILogger<FcmService> _logger;
    private readonly bool _enabled;

    public FcmService(IConfiguration configuration, ILogger<FcmService> logger)
    {
        _logger = logger;

        var credPath = configuration["Firebase:ServiceAccountPath"];
        if (string.IsNullOrEmpty(credPath) || !File.Exists(credPath))
        {
            _enabled = false;
            _logger.LogWarning("FCM disabled — Firebase:ServiceAccountPath not set or file not found.");
            return;
        }

        if (FirebaseApp.DefaultInstance is null)
        {
            FirebaseApp.Create(new AppOptions
            {
                Credential = GoogleCredential.FromFile(credPath)
            });
        }

        _enabled = true;
    }

    public async Task SendToTokenAsync(string fcmToken, string title, string body, CancellationToken cancellationToken = default)
    {
        if (!_enabled) return;
        try
        {
            await FirebaseMessaging.DefaultInstance.SendAsync(new Message
            {
                Token = fcmToken,
                Notification = new Notification { Title = title, Body = body },
                Android = new AndroidConfig
                {
                    Priority = Priority.High,
                    Notification = new AndroidNotification
                    {
                        Icon = "ic_notification",
                        Color = "#1a6b3a",
                        ChannelId = "misbahuda_default"
                    }
                }
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "FCM send failed for token {Token}", fcmToken[..10] + "...");
        }
    }

    public async Task SendToTokensAsync(IEnumerable<string> fcmTokens, string title, string body, CancellationToken cancellationToken = default)
    {
        if (!_enabled) return;

        var tokenList = fcmTokens.ToList();
        if (tokenList.Count == 0) return;

        // FCM allows max 500 tokens per batch
        foreach (var batch in tokenList.Chunk(500))
        {
            try
            {
                var response = await FirebaseMessaging.DefaultInstance.SendEachForMulticastAsync(new MulticastMessage
                {
                    Tokens = batch,
                    Notification = new Notification { Title = title, Body = body },
                    Android = new AndroidConfig
                    {
                        Priority = Priority.High,
                        Notification = new AndroidNotification
                        {
                            Icon = "ic_notification",
                            Color = "#1a6b3a",
                            ChannelId = "misbahuda_default"
                        }
                    }
                }, cancellationToken);

                _logger.LogInformation("FCM multicast: {Success} success, {Failure} failed out of {Total}",
                    response.SuccessCount, response.FailureCount, batch.Length);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FCM multicast send failed");
            }
        }
    }
}
