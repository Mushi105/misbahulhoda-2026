using Misbahuda.Domain.Common;
using Misbahuda.Domain.Enums;

namespace Misbahuda.Domain.Entities;

public class FinanceBudget : BaseEntity
{
    public ExpenseCategory Category       { get; set; }
    public decimal         BudgetedAmount { get; set; }
    public string          Currency       { get; set; } = "PKR";
    public string?         Notes          { get; set; }
}
