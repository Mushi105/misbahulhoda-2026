using Misbahuda.Domain.Common;

namespace Misbahuda.Domain.Entities;

public class ExchangeRate : BaseEntity
{
    public string  Currency   { get; set; } = string.Empty; // e.g. "USD", "GBP"
    public decimal RateToPkr  { get; set; }                 // 1 unit = X PKR
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
