namespace Misbahuda.Application.Interfaces;

public interface IAuditLogService
{
    Task LogAsync(string action, string entityName, string? entityId = null,
        string? description = null, Guid? userId = null,
        string? ipAddress = null, string? userAgent = null,
        CancellationToken cancellationToken = default);
}
