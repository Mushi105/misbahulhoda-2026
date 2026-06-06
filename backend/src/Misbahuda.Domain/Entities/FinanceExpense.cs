using Misbahuda.Domain.Common;
using Misbahuda.Domain.Enums;

namespace Misbahuda.Domain.Entities;

public class FinanceExpense : BaseEntity
{
    public ExpenseCategory Category   { get; set; }
    public ExpenseUnit     Unit       { get; set; } = ExpenseUnit.LumpSum;
    public string          Title      { get; set; } = string.Empty;
    public string?         Notes      { get; set; }
    public decimal         UnitCost   { get; set; }
    public int             Quantity   { get; set; } = 1;
    public decimal         TotalCost  { get; set; }
    public string          Currency   { get; set; } = "USD";
    public DateTime        Date       { get; set; } = DateTime.UtcNow;
    public string?         PaidTo     { get; set; }
    public Guid            CreatedById { get; set; }

    // Meal inclusion — relevant when Category == Hotel
    public bool BreakfastIncluded { get; set; }
    public bool LunchIncluded     { get; set; }
    public bool DinnerIncluded    { get; set; }
}
