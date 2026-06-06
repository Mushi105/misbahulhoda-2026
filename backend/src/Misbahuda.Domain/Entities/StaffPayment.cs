using Misbahuda.Domain.Common;
using Misbahuda.Domain.Enums;

namespace Misbahuda.Domain.Entities;

public class StaffPayment : BaseEntity
{
    public string    Name        { get; set; } = string.Empty;  // person's name
    public StaffRole Role        { get; set; }
    public string?   Phone       { get; set; }
    public string?   Description { get; set; }  // e.g. "3 majalis", "10 days work"
    public decimal   Amount      { get; set; }
    public string    Currency    { get; set; } = "USD";
    public DateTime  Date        { get; set; } = DateTime.UtcNow;
    public bool      IsPaid      { get; set; }  // paid or still pending
    public string?   Notes       { get; set; }
    public Guid      CreatedById { get; set; }
}
