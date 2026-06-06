using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Misbahuda.Application.Common;
using Misbahuda.Application.Interfaces;
using Misbahuda.Domain.Entities;
using Misbahuda.Domain.Interfaces;

namespace Misbahuda.API.Controllers;

[Authorize(Roles = "SuperAdmin,Admin,Finance")]
public class InternationalController(IMediator mediator, IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    : BaseController(mediator)
{
    private Dictionary<string, decimal> GetRates(IEnumerable<ExchangeRate> saved)
        => saved.ToDictionary(r => r.Currency, r => r.RateToPkr);

    private static decimal ToPkr(decimal amount, string currency, Dictionary<string, decimal> rates)
        => currency == "PKR" ? amount : (rates.TryGetValue(currency, out var r) ? amount * r : amount);

    // ── CURRENCY BALANCE SHEET ──────────────────────────────────────────────────
    [HttpGet("balance")]
    public async Task<IActionResult> GetBalance(CancellationToken ct)
    {
        var savedRates = await unitOfWork.ExchangeRates.GetAllAsync(ct);
        var rates      = GetRates(savedRates);
        var currencies = new[] { "USD", "GBP", "IQD", "SAR", "EUR", "PKR" };

        var incoming  = await unitOfWork.IncomingPayments.GetAllAsync(ct);
        var contracts = await unitOfWork.VendorContracts.GetAllAsync(ct);
        var transfers = await unitOfWork.MoneyTransfers.GetAllAsync(ct);
        var expenses  = await unitOfWork.FinanceExpenses.GetAllAsync(ct);

        var balance = currencies.Select(cur =>
        {
            var inAmt      = incoming.Where(x => x.Currency == cur).Sum(x => x.AmountReceived);
            var outContracts = contracts.Where(x => x.Currency == cur).Sum(x => x.PaidAmount);
            var outExpenses  = expenses.Where(x => x.Currency == cur).Sum(x => x.TotalCost);
            var transferOut  = transfers.Where(x => x.SentCurrency == cur).Sum(x => x.AmountSent);
            var transferIn   = transfers.Where(x => x.ReceivedCurrency == cur && x.Status == "Completed").Sum(x => x.AmountReceived);

            var totalIn  = inAmt + transferIn;
            var totalOut = outContracts + outExpenses + transferOut;
            var net      = totalIn - totalOut;

            return new
            {
                Currency    = cur,
                TotalIn     = Math.Round(totalIn, 2),
                TotalOut    = Math.Round(totalOut, 2),
                Net         = Math.Round(net, 2),
                NetPkr      = Math.Round(ToPkr(net, cur, rates), 2),
                Rate        = rates.TryGetValue(cur, out var rv) ? rv : (cur == "PKR" ? 1m : 0m),
            };
        }).Where(b => b.TotalIn > 0 || b.TotalOut > 0).ToList();

        var grandNetPkr = balance.Sum(b => b.NetPkr);

        return Ok(ApiResponse<object>.Ok(new { Balance = balance, GrandNetPkr = Math.Round(grandNetPkr, 2) }));
    }

    // ── INCOMING PAYMENTS ───────────────────────────────────────────────────────
    [HttpGet("incoming")]
    public async Task<IActionResult> GetIncoming(CancellationToken ct)
    {
        var savedRates = await unitOfWork.ExchangeRates.GetAllAsync(ct);
        var rates = GetRates(savedRates);
        var list = await unitOfWork.IncomingPayments.GetAllAsync(ct);

        var result = list.OrderByDescending(x => x.CreatedAt).Select(x => new
        {
            x.Id, x.GroupName, x.Country, x.PilgrimCount,
            x.AmountExpected, x.AmountReceived, x.Currency, x.Status,
            x.ReceivedDate, x.ContactName, x.ContactPhone, x.Notes,
            AmountExpectedPkr = Math.Round(ToPkr(x.AmountExpected, x.Currency, rates), 2),
            AmountReceivedPkr = Math.Round(ToPkr(x.AmountReceived, x.Currency, rates), 2),
            Remaining         = Math.Round(x.AmountExpected - x.AmountReceived, 2),
            RemainingPkr      = Math.Round(ToPkr(x.AmountExpected - x.AmountReceived, x.Currency, rates), 2),
        });

        var totalExpectedPkr = list.Sum(x => ToPkr(x.AmountExpected, x.Currency, rates));
        var totalReceivedPkr = list.Sum(x => ToPkr(x.AmountReceived, x.Currency, rates));

        return Ok(ApiResponse<object>.Ok(new
        {
            Payments          = result,
            TotalExpectedPkr  = Math.Round(totalExpectedPkr, 2),
            TotalReceivedPkr  = Math.Round(totalReceivedPkr, 2),
            TotalPendingPkr   = Math.Round(totalExpectedPkr - totalReceivedPkr, 2),
        }));
    }

    [HttpPost("incoming")]
    public async Task<IActionResult> AddIncoming([FromBody] IncomingPaymentRequest req, CancellationToken ct)
    {
        if (currentUser.UserId is null) return Unauthorized();
        var entity = new IncomingPayment
        {
            GroupName      = req.GroupName,
            Country        = req.Country,
            PilgrimCount   = req.PilgrimCount,
            AmountExpected = req.AmountExpected,
            AmountReceived = req.AmountReceived,
            Currency       = (req.Currency ?? "GBP").ToUpper(),
            ReceivedDate   = req.ReceivedDate,
            Status         = req.AmountReceived <= 0 ? "Pending" : req.AmountReceived >= req.AmountExpected ? "Full" : "Partial",
            ContactName    = req.ContactName,
            ContactPhone   = req.ContactPhone,
            Notes          = req.Notes,
            CreatedById    = currentUser.UserId.Value,
        };
        await unitOfWork.IncomingPayments.AddAsync(entity, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(new { entity.Id }, "Added."));
    }

    [HttpPut("incoming/{id}")]
    public async Task<IActionResult> UpdateIncoming(Guid id, [FromBody] IncomingPaymentRequest req, CancellationToken ct)
    {
        var entity = await unitOfWork.IncomingPayments.GetByIdAsync(id, ct);
        if (entity is null) return NotFound(ApiResponse<object>.Fail("Not found."));
        entity.GroupName      = req.GroupName;
        entity.Country        = req.Country;
        entity.PilgrimCount   = req.PilgrimCount;
        entity.AmountExpected = req.AmountExpected;
        entity.AmountReceived = req.AmountReceived;
        entity.Currency       = (req.Currency ?? "GBP").ToUpper();
        entity.ReceivedDate   = req.ReceivedDate;
        entity.Status         = req.AmountReceived <= 0 ? "Pending" : req.AmountReceived >= req.AmountExpected ? "Full" : "Partial";
        entity.ContactName    = req.ContactName;
        entity.ContactPhone   = req.ContactPhone;
        entity.Notes          = req.Notes;
        unitOfWork.IncomingPayments.Update(entity);
        await unitOfWork.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(new { }, "Updated."));
    }

    [HttpDelete("incoming/{id}")]
    public async Task<IActionResult> DeleteIncoming(Guid id, CancellationToken ct)
    {
        var entity = await unitOfWork.IncomingPayments.GetByIdAsync(id, ct);
        if (entity is null) return NotFound(ApiResponse<object>.Fail("Not found."));
        unitOfWork.IncomingPayments.Delete(entity);
        await unitOfWork.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(new { }, "Deleted."));
    }

    // ── VENDOR CONTRACTS ───────────────────────────────────────────────────────
    [HttpGet("contracts")]
    public async Task<IActionResult> GetContracts(CancellationToken ct)
    {
        var savedRates = await unitOfWork.ExchangeRates.GetAllAsync(ct);
        var rates = GetRates(savedRates);
        var list = await unitOfWork.VendorContracts.GetAllAsync(ct);

        var result = list.OrderByDescending(x => x.ContractDate).Select(x => new
        {
            x.Id, x.VendorName, x.Country, x.ServiceType, x.Description,
            x.TotalAmount, x.PaidAmount, x.Currency, x.Status,
            x.ContractDate, x.ContactName, x.ContactPhone, x.Notes,
            TotalAmountPkr = Math.Round(ToPkr(x.TotalAmount, x.Currency, rates), 2),
            PaidAmountPkr  = Math.Round(ToPkr(x.PaidAmount, x.Currency, rates), 2),
            Remaining      = Math.Round(x.TotalAmount - x.PaidAmount, 2),
            RemainingPkr   = Math.Round(ToPkr(x.TotalAmount - x.PaidAmount, x.Currency, rates), 2),
        });

        var totalValuePkr = list.Sum(x => ToPkr(x.TotalAmount, x.Currency, rates));
        var totalPaidPkr  = list.Sum(x => ToPkr(x.PaidAmount, x.Currency, rates));

        return Ok(ApiResponse<object>.Ok(new
        {
            Contracts      = result,
            TotalValuePkr  = Math.Round(totalValuePkr, 2),
            TotalPaidPkr   = Math.Round(totalPaidPkr, 2),
            TotalDuePkr    = Math.Round(totalValuePkr - totalPaidPkr, 2),
        }));
    }

    [HttpPost("contracts")]
    public async Task<IActionResult> AddContract([FromBody] VendorContractRequest req, CancellationToken ct)
    {
        if (currentUser.UserId is null) return Unauthorized();
        var entity = new VendorContract
        {
            VendorName   = req.VendorName,
            Country      = req.Country,
            ServiceType  = req.ServiceType,
            Description  = req.Description,
            TotalAmount  = req.TotalAmount,
            PaidAmount   = req.PaidAmount,
            Currency     = (req.Currency ?? "USD").ToUpper(),
            ContractDate = req.ContractDate != default ? req.ContractDate : DateTime.UtcNow,
            Status       = req.Status ?? "Active",
            ContactName  = req.ContactName,
            ContactPhone = req.ContactPhone,
            Notes        = req.Notes,
            CreatedById  = currentUser.UserId.Value,
        };
        await unitOfWork.VendorContracts.AddAsync(entity, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(new { entity.Id }, "Contract added."));
    }

    [HttpPut("contracts/{id}")]
    public async Task<IActionResult> UpdateContract(Guid id, [FromBody] VendorContractRequest req, CancellationToken ct)
    {
        var entity = await unitOfWork.VendorContracts.GetByIdAsync(id, ct);
        if (entity is null) return NotFound(ApiResponse<object>.Fail("Not found."));
        entity.VendorName   = req.VendorName;
        entity.Country      = req.Country;
        entity.ServiceType  = req.ServiceType;
        entity.Description  = req.Description;
        entity.TotalAmount  = req.TotalAmount;
        entity.PaidAmount   = req.PaidAmount;
        entity.Currency     = (req.Currency ?? "USD").ToUpper();
        entity.ContractDate = req.ContractDate != default ? req.ContractDate : entity.ContractDate;
        entity.Status       = req.Status ?? entity.Status;
        entity.ContactName  = req.ContactName;
        entity.ContactPhone = req.ContactPhone;
        entity.Notes        = req.Notes;
        unitOfWork.VendorContracts.Update(entity);
        await unitOfWork.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(new { }, "Updated."));
    }

    [HttpDelete("contracts/{id}")]
    public async Task<IActionResult> DeleteContract(Guid id, CancellationToken ct)
    {
        var entity = await unitOfWork.VendorContracts.GetByIdAsync(id, ct);
        if (entity is null) return NotFound(ApiResponse<object>.Fail("Not found."));
        unitOfWork.VendorContracts.Delete(entity);
        await unitOfWork.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(new { }, "Deleted."));
    }

    // ── MONEY TRANSFERS ────────────────────────────────────────────────────────
    [HttpGet("transfers")]
    public async Task<IActionResult> GetTransfers(CancellationToken ct)
    {
        var savedRates = await unitOfWork.ExchangeRates.GetAllAsync(ct);
        var rates = GetRates(savedRates);
        var list = await unitOfWork.MoneyTransfers.GetAllAsync(ct);

        var result = list.OrderByDescending(x => x.TransferDate).Select(x => new
        {
            x.Id, x.SentFrom, x.SentTo, x.AmountSent, x.SentCurrency,
            x.AmountReceived, x.ReceivedCurrency, x.RateUsed, x.Method,
            x.TransferDate, x.Status, x.ReferenceNumber, x.Notes,
            AmountSentPkr     = Math.Round(ToPkr(x.AmountSent, x.SentCurrency, rates), 2),
            AmountReceivedPkr = Math.Round(ToPkr(x.AmountReceived, x.ReceivedCurrency, rates), 2),
        });

        var totalSentPkr     = list.Sum(x => ToPkr(x.AmountSent, x.SentCurrency, rates));
        var completedCount   = list.Count(x => x.Status == "Completed");

        return Ok(ApiResponse<object>.Ok(new
        {
            Transfers        = result,
            TotalSentPkr     = Math.Round(totalSentPkr, 2),
            TotalCount       = list.Count(),
            CompletedCount   = completedCount,
            PendingCount     = list.Count(x => x.Status == "Pending"),
        }));
    }

    [HttpPost("transfers")]
    public async Task<IActionResult> AddTransfer([FromBody] MoneyTransferRequest req, CancellationToken ct)
    {
        if (currentUser.UserId is null) return Unauthorized();
        var entity = new MoneyTransfer
        {
            SentFrom         = req.SentFrom,
            SentTo           = req.SentTo,
            AmountSent       = req.AmountSent,
            SentCurrency     = (req.SentCurrency ?? "GBP").ToUpper(),
            AmountReceived   = req.AmountReceived,
            ReceivedCurrency = (req.ReceivedCurrency ?? "USD").ToUpper(),
            RateUsed         = req.RateUsed,
            Method           = req.Method ?? "Hawala",
            TransferDate     = req.TransferDate != default ? req.TransferDate : DateTime.UtcNow,
            Status           = req.Status ?? "Pending",
            ReferenceNumber  = req.ReferenceNumber,
            Notes            = req.Notes,
            CreatedById      = currentUser.UserId.Value,
        };
        await unitOfWork.MoneyTransfers.AddAsync(entity, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(new { entity.Id }, "Transfer added."));
    }

    [HttpPut("transfers/{id}")]
    public async Task<IActionResult> UpdateTransfer(Guid id, [FromBody] MoneyTransferRequest req, CancellationToken ct)
    {
        var entity = await unitOfWork.MoneyTransfers.GetByIdAsync(id, ct);
        if (entity is null) return NotFound(ApiResponse<object>.Fail("Not found."));
        entity.SentFrom         = req.SentFrom;
        entity.SentTo           = req.SentTo;
        entity.AmountSent       = req.AmountSent;
        entity.SentCurrency     = (req.SentCurrency ?? "GBP").ToUpper();
        entity.AmountReceived   = req.AmountReceived;
        entity.ReceivedCurrency = (req.ReceivedCurrency ?? "USD").ToUpper();
        entity.RateUsed         = req.RateUsed;
        entity.Method           = req.Method ?? entity.Method;
        entity.TransferDate     = req.TransferDate != default ? req.TransferDate : entity.TransferDate;
        entity.Status           = req.Status ?? entity.Status;
        entity.ReferenceNumber  = req.ReferenceNumber;
        entity.Notes            = req.Notes;
        unitOfWork.MoneyTransfers.Update(entity);
        await unitOfWork.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(new { }, "Updated."));
    }

    [HttpDelete("transfers/{id}")]
    public async Task<IActionResult> DeleteTransfer(Guid id, CancellationToken ct)
    {
        var entity = await unitOfWork.MoneyTransfers.GetByIdAsync(id, ct);
        if (entity is null) return NotFound(ApiResponse<object>.Fail("Not found."));
        unitOfWork.MoneyTransfers.Delete(entity);
        await unitOfWork.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(new { }, "Deleted."));
    }
}

public record IncomingPaymentRequest(
    string    GroupName, string Country, int PilgrimCount,
    decimal   AmountExpected, decimal AmountReceived, string? Currency,
    DateTime? ReceivedDate, string? ContactName, string? ContactPhone, string? Notes
);

public record VendorContractRequest(
    string   VendorName, string Country, string ServiceType,
    decimal  TotalAmount, decimal PaidAmount, string? Currency,
    DateTime ContractDate, string? Status, string? Description,
    string?  ContactName, string? ContactPhone, string? Notes
);

public record MoneyTransferRequest(
    string   SentFrom, string SentTo,
    decimal  AmountSent, string? SentCurrency,
    decimal  AmountReceived, string? ReceivedCurrency,
    decimal  RateUsed, string? Method, DateTime TransferDate,
    string?  Status, string? ReferenceNumber, string? Notes
);
