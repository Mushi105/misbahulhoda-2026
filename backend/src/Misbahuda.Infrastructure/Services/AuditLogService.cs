using Misbahuda.Application.Interfaces;
using Misbahuda.Domain.Entities;
using Misbahuda.Domain.Interfaces;

namespace Misbahuda.Infrastructure.Services;

public class AuditLogService(IUnitOfWork unitOfWork) : IAuditLogService
{
    public async Task LogAsync(string action, string entityName, string? entityId = null,
        string? description = null, Guid? userId = null,
        string? ipAddress = null, string? userAgent = null,
        CancellationToken cancellationToken = default)
    {
        var log = new AuditLog
        {
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            NewValues = description,
            UserId = userId,
            IpAddress = ipAddress,
            UserAgent = userAgent
        };

        await unitOfWork.AuditLogs.AddAsync(log, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
