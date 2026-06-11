using Microsoft.EntityFrameworkCore.Storage;
using Misbahuda.Domain.Entities;
using Misbahuda.Domain.Interfaces;
using Misbahuda.Infrastructure.Data;

namespace Misbahuda.Infrastructure.Repositories;

public class UnitOfWork(ApplicationDbContext context) : IUnitOfWork
{
    private IDbContextTransaction? _transaction;

    public IRepository<User> Users { get; } = new Repository<User>(context);
    public IRepository<RefreshToken> RefreshTokens { get; } = new Repository<RefreshToken>(context);
    public IRepository<Pilgrim> Pilgrims { get; } = new Repository<Pilgrim>(context);
    public IRepository<Volunteer> Volunteers { get; } = new Repository<Volunteer>(context);
    public IRepository<VolunteerTask> VolunteerTasks { get; } = new Repository<VolunteerTask>(context);
    public IRepository<Attendance> Attendances { get; } = new Repository<Attendance>(context);
    public IRepository<Hotel> Hotels { get; } = new Repository<Hotel>(context);
    public IRepository<Building> Buildings { get; } = new Repository<Building>(context);
    public IRepository<Floor> Floors { get; } = new Repository<Floor>(context);
    public IRepository<Room> Rooms { get; } = new Repository<Room>(context);
    public IRepository<Bus> Buses { get; } = new Repository<Bus>(context);
    public IRepository<Karwan> Karwans { get; } = new Repository<Karwan>(context);
    public IRepository<GpsLocation> GpsLocations { get; } = new Repository<GpsLocation>(context);
    public IRepository<Majalis> Majalis { get; } = new Repository<Majalis>(context);
    public IRepository<MajalisProgram> MajalisPrograms { get; } = new Repository<MajalisProgram>(context);
    public IRepository<NamazTiming> NamazTimings { get; } = new Repository<NamazTiming>(context);
    public IRepository<FoodSchedule> FoodSchedules { get; } = new Repository<FoodSchedule>(context);
    public IRepository<Notification> Notifications { get; } = new Repository<Notification>(context);
    public IRepository<FileAttachment> FileAttachments { get; } = new Repository<FileAttachment>(context);
    public IRepository<FamilyMember> FamilyMembers { get; } = new Repository<FamilyMember>(context);
    public IRepository<Notice> Notices { get; } = new Repository<Notice>(context);
    public IRepository<TourDocument> TourDocuments { get; } = new Repository<TourDocument>(context);
    public IRepository<Zone> Zones { get; } = new Repository<Zone>(context);
    public IRepository<Scholar> Scholars { get; } = new Repository<Scholar>(context);
    public IRepository<VolunteerPilgrimAssignment> VolunteerPilgrimAssignments { get; } = new Repository<VolunteerPilgrimAssignment>(context);
    public IRepository<AuditLog> AuditLogs { get; } = new Repository<AuditLog>(context);
    public IRepository<PilgrimRoomHistory> PilgrimRoomHistories { get; } = new Repository<PilgrimRoomHistory>(context);
    public IRepository<FinanceExpense> FinanceExpenses { get; } = new Repository<FinanceExpense>(context);
    public IRepository<FinanceBudget> FinanceBudgets { get; } = new Repository<FinanceBudget>(context);
    public IRepository<ExchangeRate> ExchangeRates { get; } = new Repository<ExchangeRate>(context);
    public IRepository<StaffPayment> StaffPayments { get; } = new Repository<StaffPayment>(context);
    public IRepository<TravelTicket>    TravelTickets    { get; } = new Repository<TravelTicket>(context);
    public IRepository<IncomingPayment> IncomingPayments { get; } = new Repository<IncomingPayment>(context);
    public IRepository<VendorContract>  VendorContracts  { get; } = new Repository<VendorContract>(context);
    public IRepository<MoneyTransfer>    MoneyTransfers    { get; } = new Repository<MoneyTransfer>(context);
    public IRepository<AirportTransfer>  AirportTransfers  { get; } = new Repository<AirportTransfer>(context);
    public IRepository<Tour>             Tours             { get; } = new Repository<Tour>(context);
    public IRepository<TourPackage>      TourPackages      { get; } = new Repository<TourPackage>(context);
    public IRepository<TourDaySchedule>  TourDaySchedules  { get; } = new Repository<TourDaySchedule>(context);
    public IRepository<PilgrimFeedback>  PilgrimFeedbacks  { get; } = new Repository<PilgrimFeedback>(context);
    public IRepository<DeviceToken>      DeviceTokens      { get; } = new Repository<DeviceToken>(context);

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        await context.SaveChangesAsync(cancellationToken);

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default) =>
        _transaction = await context.Database.BeginTransactionAsync(cancellationToken);

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is not null)
            await _transaction.CommitAsync(cancellationToken);
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is not null)
            await _transaction.RollbackAsync(cancellationToken);
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        context.Dispose();
    }
}
