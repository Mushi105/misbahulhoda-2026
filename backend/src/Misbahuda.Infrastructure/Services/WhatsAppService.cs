using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace Misbahuda.Infrastructure.Services;

public interface IWhatsAppService
{
    Task<bool> SendMessageAsync(string toPhone, string message);
    Task<int> BroadcastAsync(IEnumerable<string> phones, string message);
}

public class WhatsAppService(IConfiguration config, ILogger<WhatsAppService> logger) : IWhatsAppService
{
    private readonly string? _accountSid = config["Twilio:AccountSid"];
    private readonly string? _authToken = config["Twilio:AuthToken"];
    private readonly string? _fromNumber = config["Twilio:WhatsAppFrom"]; // e.g. whatsapp:+14155238886

    public async Task<bool> SendMessageAsync(string toPhone, string message)
    {
        if (string.IsNullOrEmpty(_accountSid) || string.IsNullOrEmpty(_authToken) || string.IsNullOrEmpty(_fromNumber))
        {
            logger.LogWarning("Twilio not configured. Message not sent to {Phone}", toPhone);
            return false;
        }

        try
        {
            TwilioClient.Init(_accountSid, _authToken);
            var normalized = toPhone.StartsWith("+") ? toPhone : "+" + toPhone.TrimStart('0');
            await MessageResource.CreateAsync(
                to: new PhoneNumber($"whatsapp:{normalized}"),
                from: new PhoneNumber(_fromNumber),
                body: message);
            logger.LogInformation("WhatsApp sent to {Phone}", normalized);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "WhatsApp failed for {Phone}", toPhone);
            return false;
        }
    }

    public async Task<int> BroadcastAsync(IEnumerable<string> phones, string message)
    {
        var tasks = phones.Select(p => SendMessageAsync(p, message));
        var results = await Task.WhenAll(tasks);
        return results.Count(r => r);
    }
}
