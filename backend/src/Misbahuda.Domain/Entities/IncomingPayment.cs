using Misbahuda.Domain.Common;

namespace Misbahuda.Domain.Entities;

public class IncomingPayment : BaseEntity
{
    public string    GroupName      { get; set; } = string.Empty;
    public string    Country        { get; set; } = string.Empty;
    public int       PilgrimCount   { get; set; }
    public decimal   AmountExpected { get; set; }
    public decimal   AmountReceived { get; set; }
    public string    Currency       { get; set; } = "GBP";
    public DateTime? ReceivedDate   { get; set; }
    public string    Status         { get; set; } = "Pending"; // Pending, Partial, Full
    public string?   ContactName    { get; set; }
    public string?   ContactPhone   { get; set; }
    public string?   Notes          { get; set; }
    public Guid      CreatedById    { get; set; }
}
