using Misbahuda.Domain.Common;

namespace Misbahuda.Domain.Entities;

public class VendorContract : BaseEntity
{
    public string   VendorName    { get; set; } = string.Empty;
    public string   Country       { get; set; } = string.Empty;
    public string   ServiceType   { get; set; } = string.Empty; // Hotel, Transport, Visa, Food, Other
    public string?  Description   { get; set; }
    public decimal  TotalAmount   { get; set; }
    public decimal  PaidAmount    { get; set; }
    public string   Currency      { get; set; } = "USD";
    public DateTime ContractDate  { get; set; } = DateTime.UtcNow;
    public string   Status        { get; set; } = "Active"; // Active, Completed, Cancelled
    public string?  ContactName   { get; set; }
    public string?  ContactPhone  { get; set; }
    public string?  Notes         { get; set; }
    public Guid     CreatedById   { get; set; }
}
