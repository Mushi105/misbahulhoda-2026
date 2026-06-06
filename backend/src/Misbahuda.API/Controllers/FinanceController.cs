using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Misbahuda.Application.Common;
using Misbahuda.Application.Interfaces;
using Misbahuda.Domain.Entities;
using Misbahuda.Domain.Enums;
using Misbahuda.Domain.Interfaces;

namespace Misbahuda.API.Controllers;

[Authorize(Roles = "SuperAdmin,Admin,Finance")]
public class FinanceController(IMediator mediator, IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    : BaseController(mediator)
{
    private static readonly Dictionary<ExpenseCategory, string> CategoryLabels = new()
    {
        [ExpenseCategory.Hotel]         = "Hotel",
        [ExpenseCategory.Breakfast]     = "Breakfast",
        [ExpenseCategory.Lunch]         = "Lunch",
        [ExpenseCategory.Dinner]        = "Dinner",
        [ExpenseCategory.BusRent]       = "Bus Rent",
        [ExpenseCategory.Fuel]          = "Fuel",
        [ExpenseCategory.Accommodation] = "Accommodation",
        [ExpenseCategory.Medical]       = "Medical",
        [ExpenseCategory.VisaAdmin]     = "Visa / Admin",
        [ExpenseCategory.Miscellaneous] = "Miscellaneous",
    };

    // Supported currencies with display names
    public static readonly Dictionary<string, string> SupportedCurrencies = new()
    {
        ["PKR"] = "Pakistani Rupee",
        ["USD"] = "US Dollar",
        ["GBP"] = "British Pound",
        ["IQD"] = "Iraqi Dinar",
        ["SAR"] = "Saudi Riyal",
        ["EUR"] = "Euro",
    };

    private static decimal ToPkr(decimal amount, string currency, Dictionary<string, decimal> rates)
    {
        if (currency == "PKR") return amount;
        return rates.TryGetValue(currency, out var rate) ? amount * rate : amount;
    }

    // ── EXCHANGE RATES ──────────────────────────────────────────────────────────
    [HttpGet("rates")]
    public async Task<IActionResult> GetRates(CancellationToken cancellationToken)
    {
        var saved = await unitOfWork.ExchangeRates.GetAllAsync(cancellationToken);
        var rateMap = saved.ToDictionary(r => r.Currency, r => r.RateToPkr);

        var result = SupportedCurrencies.Select(kv => new
        {
            Currency    = kv.Key,
            Name        = kv.Value,
            RateToPkr   = rateMap.TryGetValue(kv.Key, out var r) ? r : (kv.Key == "PKR" ? 1m : 0m),
            UpdatedAt   = saved.FirstOrDefault(x => x.Currency == kv.Key)?.UpdatedAt,
        });

        return Ok(ApiResponse<object>.Ok(result));
    }

    [HttpPost("rates")]
    public async Task<IActionResult> SetRate([FromBody] ExchangeRateRequest req, CancellationToken cancellationToken)
    {
        if (req.Currency == "PKR") return BadRequest(ApiResponse<object>.Fail("PKR is the base currency, rate is always 1."));

        var existing = (await unitOfWork.ExchangeRates.FindAsync(
            r => r.Currency == req.Currency, cancellationToken)).FirstOrDefault();

        if (existing is not null)
        {
            existing.RateToPkr = req.RateToPkr;
            existing.UpdatedAt = DateTime.UtcNow;
            unitOfWork.ExchangeRates.Update(existing);
        }
        else
        {
            await unitOfWork.ExchangeRates.AddAsync(new ExchangeRate
            {
                Currency  = req.Currency,
                RateToPkr = req.RateToPkr,
                UpdatedAt = DateTime.UtcNow,
            }, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { }, $"Rate for {req.Currency} saved."));
    }

    // ── SUMMARY ────────────────────────────────────────────────────────────────
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken)
    {
        var expenses  = await unitOfWork.FinanceExpenses.GetAllAsync(cancellationToken);
        var budgets   = await unitOfWork.FinanceBudgets.GetAllAsync(cancellationToken);
        var savedRates = await unitOfWork.ExchangeRates.GetAllAsync(cancellationToken);
        var rates     = savedRates.ToDictionary(r => r.Currency, r => r.RateToPkr);

        var expenseList = expenses.ToList();

        // Convert everything to PKR for totals
        var totalSpentPkr  = expenseList.Sum(e => ToPkr(e.TotalCost, e.Currency, rates));
        var totalBudgetPkr = budgets.Sum(b => ToPkr(b.BudgetedAmount, b.Currency, rates));

        var byCategory = Enum.GetValues<ExpenseCategory>().Select(cat =>
        {
            var catExpenses = expenseList.Where(e => e.Category == cat).ToList();
            var spentPkr    = catExpenses.Sum(e => ToPkr(e.TotalCost, e.Currency, rates));
            var budget      = budgets.FirstOrDefault(b => b.Category == cat);
            var budgetPkr   = budget is not null ? ToPkr(budget.BudgetedAmount, budget.Currency, rates) : 0m;
            return new
            {
                Category      = cat.ToString(),
                Label         = CategoryLabels[cat],
                SpentPkr      = Math.Round(spentPkr, 2),
                BudgetPkr     = Math.Round(budgetPkr, 2),
                RemainingPkr  = Math.Round(budgetPkr - spentPkr, 2),
                Count         = catExpenses.Count,
            };
        }).Where(c => c.SpentPkr > 0 || c.BudgetPkr > 0).ToList();

        return Ok(ApiResponse<object>.Ok(new
        {
            TotalBudgetPkr  = Math.Round(totalBudgetPkr, 2),
            TotalSpentPkr   = Math.Round(totalSpentPkr, 2),
            TotalSavingPkr  = Math.Round(totalBudgetPkr - totalSpentPkr, 2),
            ExpenseCount    = expenseList.Count,
            ByCategory      = byCategory,
            RecentExpenses  = expenseList
                .OrderByDescending(e => e.Date)
                .Take(5)
                .Select(e => new
                {
                    e.Id, e.Title, e.Category, Label = CategoryLabels[e.Category],
                    e.UnitCost, e.Quantity, e.TotalCost, e.Currency,
                    TotalCostPkr = Math.Round(ToPkr(e.TotalCost, e.Currency, rates), 2),
                    e.Date, e.PaidTo
                })
        }));
    }

    // ── LIST EXPENSES ──────────────────────────────────────────────────────────
    [HttpGet("expenses")]
    public async Task<IActionResult> GetExpenses(
        [FromQuery] ExpenseCategory? category,
        [FromQuery] string? from,
        [FromQuery] string? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        var all        = await unitOfWork.FinanceExpenses.GetAllAsync(cancellationToken);
        var savedRates = await unitOfWork.ExchangeRates.GetAllAsync(cancellationToken);
        var rates      = savedRates.ToDictionary(r => r.Currency, r => r.RateToPkr);

        if (category.HasValue) all = all.Where(e => e.Category == category);
        if (DateTime.TryParse(from, out var fromDate)) all = all.Where(e => e.Date >= fromDate);
        if (DateTime.TryParse(to, out var toDate))     all = all.Where(e => e.Date <= toDate.AddDays(1));

        var ordered = all.OrderByDescending(e => e.Date).ToList();
        var total   = ordered.Count;
        var items   = ordered.Skip((page - 1) * pageSize).Take(pageSize).Select(e => new
        {
            e.Id, e.Title, e.Notes, e.PaidTo, e.Currency,
            Category          = e.Category.ToString(),
            Label             = CategoryLabels[e.Category],
            Unit              = e.Unit.ToString(),
            e.UnitCost, e.Quantity, e.TotalCost,
            TotalCostPkr      = Math.Round(ToPkr(e.TotalCost, e.Currency, rates), 2),
            e.Date, e.CreatedAt,
            e.BreakfastIncluded, e.LunchIncluded, e.DinnerIncluded,
        });

        return Ok(ApiResponse<object>.Ok(new { Items = items, TotalCount = total, Page = page, PageSize = pageSize }));
    }

    // ── ADD EXPENSE ────────────────────────────────────────────────────────────
    [HttpPost("expenses")]
    public async Task<IActionResult> AddExpense([FromBody] ExpenseRequest req, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null) return Unauthorized();

        var qty     = req.Quantity > 0 ? req.Quantity : 1;
        var expense = new FinanceExpense
        {
            Category           = req.Category,
            Unit               = req.Unit,
            Title              = req.Title,
            Notes              = req.Notes,
            UnitCost           = req.UnitCost,
            Quantity           = qty,
            TotalCost          = req.UnitCost * qty,
            Currency           = string.IsNullOrWhiteSpace(req.Currency) ? "USD" : req.Currency.ToUpper(),
            Date               = req.Date != default ? req.Date : DateTime.UtcNow,
            PaidTo             = req.PaidTo,
            CreatedById        = currentUser.UserId.Value,
            BreakfastIncluded  = req.BreakfastIncluded,
            LunchIncluded      = req.LunchIncluded,
            DinnerIncluded     = req.DinnerIncluded,
        };

        await unitOfWork.FinanceExpenses.AddAsync(expense, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.Ok(new { expense.Id, expense.TotalCost, expense.Currency }, "Expense added."));
    }

    // ── UPDATE EXPENSE ─────────────────────────────────────────────────────────
    [HttpPut("expenses/{id}")]
    public async Task<IActionResult> UpdateExpense(Guid id, [FromBody] ExpenseRequest req, CancellationToken cancellationToken)
    {
        var expense = await unitOfWork.FinanceExpenses.GetByIdAsync(id, cancellationToken);
        if (expense is null) return NotFound(ApiResponse<object>.Fail("Expense not found."));

        var qty = req.Quantity > 0 ? req.Quantity : 1;
        expense.Category  = req.Category;
        expense.Unit      = req.Unit;
        expense.Title     = req.Title;
        expense.Notes     = req.Notes;
        expense.UnitCost  = req.UnitCost;
        expense.Quantity  = qty;
        expense.TotalCost = req.UnitCost * qty;
        expense.Currency           = string.IsNullOrWhiteSpace(req.Currency) ? "USD" : req.Currency.ToUpper();
        expense.Date               = req.Date != default ? req.Date : expense.Date;
        expense.PaidTo             = req.PaidTo;
        expense.BreakfastIncluded  = req.BreakfastIncluded;
        expense.LunchIncluded      = req.LunchIncluded;
        expense.DinnerIncluded     = req.DinnerIncluded;

        unitOfWork.FinanceExpenses.Update(expense);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.Ok(new { expense.TotalCost, expense.Currency }, "Expense updated."));
    }

    // ── DELETE EXPENSE ─────────────────────────────────────────────────────────
    [HttpDelete("expenses/{id}")]
    public async Task<IActionResult> DeleteExpense(Guid id, CancellationToken cancellationToken)
    {
        var expense = await unitOfWork.FinanceExpenses.GetByIdAsync(id, cancellationToken);
        if (expense is null) return NotFound(ApiResponse<object>.Fail("Expense not found."));

        unitOfWork.FinanceExpenses.Delete(expense);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.Ok(new { }, "Expense deleted."));
    }

    // ── BUDGET ─────────────────────────────────────────────────────────────────
    [HttpGet("budgets")]
    public async Task<IActionResult> GetBudgets(CancellationToken cancellationToken)
    {
        var budgets    = await unitOfWork.FinanceBudgets.GetAllAsync(cancellationToken);
        var savedRates = await unitOfWork.ExchangeRates.GetAllAsync(cancellationToken);
        var rates      = savedRates.ToDictionary(r => r.Currency, r => r.RateToPkr);

        var result = budgets.Select(b => new
        {
            b.Id, b.BudgetedAmount, b.Currency, b.Notes,
            BudgetedAmountPkr = Math.Round(ToPkr(b.BudgetedAmount, b.Currency, rates), 2),
            Category = b.Category.ToString(),
            Label    = CategoryLabels[b.Category]
        });
        return Ok(ApiResponse<object>.Ok(result));
    }

    [HttpPost("budgets")]
    public async Task<IActionResult> SetBudget([FromBody] BudgetRequest req, CancellationToken cancellationToken)
    {
        var existing = (await unitOfWork.FinanceBudgets.FindAsync(
            b => b.Category == req.Category, cancellationToken)).FirstOrDefault();

        var currency = string.IsNullOrWhiteSpace(req.Currency) ? "USD" : req.Currency.ToUpper();

        if (existing is not null)
        {
            existing.BudgetedAmount = req.BudgetedAmount;
            existing.Currency       = currency;
            existing.Notes          = req.Notes;
            unitOfWork.FinanceBudgets.Update(existing);
        }
        else
        {
            await unitOfWork.FinanceBudgets.AddAsync(new FinanceBudget
            {
                Category       = req.Category,
                BudgetedAmount = req.BudgetedAmount,
                Currency       = currency,
                Notes          = req.Notes
            }, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { }, "Budget saved."));
    }

    // ── EXPORT DATA ────────────────────────────────────────────────────────────
    [HttpGet("export")]
    public async Task<IActionResult> Export(CancellationToken cancellationToken)
    {
        var expenses   = await unitOfWork.FinanceExpenses.GetAllAsync(cancellationToken);
        var budgets    = await unitOfWork.FinanceBudgets.GetAllAsync(cancellationToken);
        var savedRates = await unitOfWork.ExchangeRates.GetAllAsync(cancellationToken);
        var rates      = savedRates.ToDictionary(r => r.Currency, r => r.RateToPkr);
        var bMap       = budgets.ToDictionary(b => b.Category, b => ToPkr(b.BudgetedAmount, b.Currency, rates));

        var rows = expenses.OrderBy(e => e.Category).ThenByDescending(e => e.Date).Select((e, i) => new
        {
            No           = i + 1,
            Date         = e.Date.ToString("dd/MM/yyyy"),
            Category     = CategoryLabels[e.Category],
            Title        = e.Title,
            Unit         = e.Unit.ToString(),
            UnitCost     = e.UnitCost,
            Quantity     = e.Quantity,
            TotalCost    = e.TotalCost,
            Currency     = e.Currency,
            TotalCostPkr = Math.Round(ToPkr(e.TotalCost, e.Currency, rates), 2),
            PaidTo       = e.PaidTo ?? "",
            Notes        = e.Notes ?? "",
            BudgetPkr    = Math.Round(bMap.TryGetValue(e.Category, out var b) ? b : 0, 2),
        });

        return Ok(ApiResponse<object>.Ok(rows));
    }

    // ── STAFF PAYMENTS ─────────────────────────────────────────────────────────
    private static readonly Dictionary<StaffRole, string> RoleLabels = new()
    {
        [StaffRole.Molana]           = "Molana / Scholar",
        [StaffRole.PartTimeEmployee] = "Part-Time Employee",
        [StaffRole.Servant]          = "Servant / Staff",
        [StaffRole.Driver]           = "Driver",
        [StaffRole.Guide]            = "Tour Guide",
        [StaffRole.Cook]             = "Cook",
        [StaffRole.Security]         = "Security",
        [StaffRole.Other]            = "Other",
    };

    [HttpGet("staff")]
    public async Task<IActionResult> GetStaffPayments(CancellationToken cancellationToken)
    {
        var payments   = await unitOfWork.StaffPayments.GetAllAsync(cancellationToken);
        var savedRates = await unitOfWork.ExchangeRates.GetAllAsync(cancellationToken);
        var rates      = savedRates.ToDictionary(r => r.Currency, r => r.RateToPkr);

        var list = payments.OrderByDescending(p => p.Date).Select(p => new
        {
            p.Id, p.Name, p.Phone, p.Description, p.Amount, p.Currency,
            AmountPkr   = Math.Round(ToPkr(p.Amount, p.Currency, rates), 2),
            Role        = p.Role.ToString(),
            RoleLabel   = RoleLabels[p.Role],
            p.IsPaid, p.Notes, p.Date, p.CreatedAt,
        });

        var totalPkr   = payments.Sum(p => ToPkr(p.Amount, p.Currency, rates));
        var paidPkr    = payments.Where(p => p.IsPaid).Sum(p => ToPkr(p.Amount, p.Currency, rates));
        var pendingPkr = totalPkr - paidPkr;

        return Ok(ApiResponse<object>.Ok(new
        {
            Payments   = list,
            TotalPkr   = Math.Round(totalPkr, 2),
            PaidPkr    = Math.Round(paidPkr, 2),
            PendingPkr = Math.Round(pendingPkr, 2),
        }));
    }

    [HttpPost("staff")]
    public async Task<IActionResult> AddStaffPayment([FromBody] StaffPaymentRequest req, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null) return Unauthorized();

        var payment = new StaffPayment
        {
            Name        = req.Name,
            Role        = req.Role,
            Phone       = req.Phone,
            Description = req.Description,
            Amount      = req.Amount,
            Currency    = string.IsNullOrWhiteSpace(req.Currency) ? "USD" : req.Currency.ToUpper(),
            Date        = req.Date != default ? req.Date : DateTime.UtcNow,
            IsPaid      = req.IsPaid,
            Notes       = req.Notes,
            CreatedById = currentUser.UserId.Value,
        };

        await unitOfWork.StaffPayments.AddAsync(payment, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { payment.Id }, "Staff payment added."));
    }

    [HttpPut("staff/{id}")]
    public async Task<IActionResult> UpdateStaffPayment(Guid id, [FromBody] StaffPaymentRequest req, CancellationToken cancellationToken)
    {
        var payment = await unitOfWork.StaffPayments.GetByIdAsync(id, cancellationToken);
        if (payment is null) return NotFound(ApiResponse<object>.Fail("Not found."));

        payment.Name        = req.Name;
        payment.Role        = req.Role;
        payment.Phone       = req.Phone;
        payment.Description = req.Description;
        payment.Amount      = req.Amount;
        payment.Currency    = string.IsNullOrWhiteSpace(req.Currency) ? "USD" : req.Currency.ToUpper();
        payment.Date        = req.Date != default ? req.Date : payment.Date;
        payment.IsPaid      = req.IsPaid;
        payment.Notes       = req.Notes;

        unitOfWork.StaffPayments.Update(payment);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { }, "Updated."));
    }

    [HttpPatch("staff/{id}/mark-paid")]
    public async Task<IActionResult> MarkPaid(Guid id, CancellationToken cancellationToken)
    {
        var payment = await unitOfWork.StaffPayments.GetByIdAsync(id, cancellationToken);
        if (payment is null) return NotFound(ApiResponse<object>.Fail("Not found."));
        payment.IsPaid = true;
        unitOfWork.StaffPayments.Update(payment);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { }, "Marked as paid."));
    }

    [HttpDelete("staff/{id}")]
    public async Task<IActionResult> DeleteStaffPayment(Guid id, CancellationToken cancellationToken)
    {
        var payment = await unitOfWork.StaffPayments.GetByIdAsync(id, cancellationToken);
        if (payment is null) return NotFound(ApiResponse<object>.Fail("Not found."));
        unitOfWork.StaffPayments.Delete(payment);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { }, "Deleted."));
    }

    // ── TRAVEL TICKETS ─────────────────────────────────────────────────────────
    private static readonly Dictionary<TicketStatus, string> StatusLabels = new()
    {
        [TicketStatus.Pending]   = "Pending",
        [TicketStatus.Booked]    = "Booked",
        [TicketStatus.Used]      = "Used",
        [TicketStatus.Cancelled] = "Cancelled",
    };

    [HttpGet("tickets")]
    public async Task<IActionResult> GetTickets(CancellationToken cancellationToken)
    {
        var tickets    = await unitOfWork.TravelTickets.GetAllAsync(cancellationToken);
        var savedRates = await unitOfWork.ExchangeRates.GetAllAsync(cancellationToken);
        var rates      = savedRates.ToDictionary(r => r.Currency, r => r.RateToPkr);

        var list = tickets.OrderByDescending(t => t.TravelDate).Select(t => new
        {
            t.Id, t.PassengerName, t.Phone, t.FromCity, t.ToCity,
            t.Airline, t.TicketNumber, t.FlightNumber, t.Cost, t.Currency,
            CostPkr       = Math.Round(ToPkr(t.Cost, t.Currency, rates), 2),
            PassengerRole = t.PassengerRole.ToString(),
            RoleLabel     = RoleLabels[t.PassengerRole],
            TicketType    = t.TicketType.ToString(),
            Status        = t.Status.ToString(),
            StatusLabel   = StatusLabels[t.Status],
            t.TravelDate, t.Notes, t.CreatedAt,
        });

        var totalCostPkr     = tickets.Sum(t => ToPkr(t.Cost, t.Currency, rates));
        var bookedCount      = tickets.Count(t => t.Status == TicketStatus.Booked || t.Status == TicketStatus.Used);
        var pendingCount     = tickets.Count(t => t.Status == TicketStatus.Pending);

        return Ok(ApiResponse<object>.Ok(new
        {
            Tickets      = list,
            TotalCostPkr = Math.Round(totalCostPkr, 2),
            TotalCount   = tickets.Count(),
            BookedCount  = bookedCount,
            PendingCount = pendingCount,
        }));
    }

    [HttpPost("tickets")]
    public async Task<IActionResult> AddTicket([FromBody] TicketRequest req, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null) return Unauthorized();

        var ticket = new TravelTicket
        {
            PassengerName  = req.PassengerName,
            PassengerRole  = req.PassengerRole,
            Phone          = req.Phone,
            TicketType     = req.TicketType,
            FromCity       = req.FromCity,
            ToCity         = req.ToCity,
            TravelDate     = req.TravelDate,
            Airline        = req.Airline,
            TicketNumber   = req.TicketNumber,
            FlightNumber   = req.FlightNumber,
            Cost           = req.Cost,
            Currency       = string.IsNullOrWhiteSpace(req.Currency) ? "USD" : req.Currency.ToUpper(),
            Status         = req.Status,
            Notes          = req.Notes,
            CreatedById    = currentUser.UserId.Value,
        };

        await unitOfWork.TravelTickets.AddAsync(ticket, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { ticket.Id }, "Ticket added."));
    }

    [HttpPut("tickets/{id}")]
    public async Task<IActionResult> UpdateTicket(Guid id, [FromBody] TicketRequest req, CancellationToken cancellationToken)
    {
        var ticket = await unitOfWork.TravelTickets.GetByIdAsync(id, cancellationToken);
        if (ticket is null) return NotFound(ApiResponse<object>.Fail("Not found."));

        ticket.PassengerName  = req.PassengerName;
        ticket.PassengerRole  = req.PassengerRole;
        ticket.Phone          = req.Phone;
        ticket.TicketType     = req.TicketType;
        ticket.FromCity       = req.FromCity;
        ticket.ToCity         = req.ToCity;
        ticket.TravelDate     = req.TravelDate;
        ticket.Airline        = req.Airline;
        ticket.TicketNumber   = req.TicketNumber;
        ticket.FlightNumber   = req.FlightNumber;
        ticket.Cost           = req.Cost;
        ticket.Currency       = string.IsNullOrWhiteSpace(req.Currency) ? "USD" : req.Currency.ToUpper();
        ticket.Status         = req.Status;
        ticket.Notes          = req.Notes;

        unitOfWork.TravelTickets.Update(ticket);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { }, "Updated."));
    }

    [HttpDelete("tickets/{id}")]
    public async Task<IActionResult> DeleteTicket(Guid id, CancellationToken cancellationToken)
    {
        var ticket = await unitOfWork.TravelTickets.GetByIdAsync(id, cancellationToken);
        if (ticket is null) return NotFound(ApiResponse<object>.Fail("Not found."));
        unitOfWork.TravelTickets.Delete(ticket);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { }, "Deleted."));
    }
}

public record ExpenseRequest(
    ExpenseCategory Category,
    ExpenseUnit     Unit,
    string          Title,
    decimal         UnitCost,
    int             Quantity,
    string?         Currency,
    DateTime        Date,
    string?         PaidTo,
    string?         Notes,
    bool            BreakfastIncluded = false,
    bool            LunchIncluded     = false,
    bool            DinnerIncluded    = false
);

public record BudgetRequest(
    ExpenseCategory Category,
    decimal         BudgetedAmount,
    string?         Currency,
    string?         Notes
);

public record ExchangeRateRequest(
    string  Currency,
    decimal RateToPkr
);

public record StaffPaymentRequest(
    string    Name,
    StaffRole Role,
    decimal   Amount,
    string?   Currency,
    DateTime  Date,
    bool      IsPaid,
    string?   Phone,
    string?   Description,
    string?   Notes
);

public record TicketRequest(
    string       PassengerName,
    StaffRole    PassengerRole,
    TicketType   TicketType,
    string       FromCity,
    string       ToCity,
    DateTime     TravelDate,
    decimal      Cost,
    TicketStatus Status,
    string?      Currency,
    string?      Phone,
    string?      Airline,
    string?      TicketNumber,
    string?      FlightNumber,
    string?      Notes
);
