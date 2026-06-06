using Misbahuda.Domain.Common;
using Misbahuda.Domain.Enums;

namespace Misbahuda.Domain.Entities;

public class AirportTransfer : BaseEntity
{
    public Guid           PilgrimId          { get; set; }
    public Pilgrim        Pilgrim            { get; set; } = null!;
    public TransferType   TransferType       { get; set; }
    public TransferStatus Status             { get; set; } = TransferStatus.Pending;
    public string?        DriverName         { get; set; }
    public string?        DriverPhone        { get; set; }
    public string?        VehicleNumber      { get; set; }
    public string?        VehicleType        { get; set; }  // Car, Van, Coaster, Bus
    public string?        MeetingPoint       { get; set; }  // where driver meets pilgrim
    public DateTime?      ScheduledTime      { get; set; }  // when driver will arrive
    public string?        Notes              { get; set; }
    public DateTime?      CompletedAt        { get; set; }
}
