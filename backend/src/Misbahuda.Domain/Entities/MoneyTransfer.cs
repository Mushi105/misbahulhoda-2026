using Misbahuda.Domain.Common;

namespace Misbahuda.Domain.Entities;

public class MoneyTransfer : BaseEntity
{
    public string   SentFrom         { get; set; } = string.Empty; // e.g. "UK - Barclays"
    public string   SentTo           { get; set; } = string.Empty; // e.g. "Iraq - Najaf Agent"
    public decimal  AmountSent       { get; set; }
    public string   SentCurrency     { get; set; } = "GBP";
    public decimal  AmountReceived   { get; set; }
    public string   ReceivedCurrency { get; set; } = "USD";
    public decimal  RateUsed         { get; set; }   // 1 SentCurrency = X ReceivedCurrency
    public string   Method           { get; set; } = "Hawala"; // Hawala, Wire, Cash, Crypto
    public DateTime TransferDate     { get; set; } = DateTime.UtcNow;
    public string   Status           { get; set; } = "Pending"; // Pending, Completed, Failed
    public string?  ReferenceNumber  { get; set; }
    public string?  Notes            { get; set; }
    public Guid     CreatedById      { get; set; }
}
