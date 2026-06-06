using Misbahuda.Domain.Entities;

namespace Misbahuda.Domain.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IRepository<User> Users { get; }
    IRepository<RefreshToken> RefreshTokens { get; }
    IRepository<Pilgrim> Pilgrims { get; }
    IRepository<Volunteer> Volunteers { get; }
    IRepository<VolunteerTask> VolunteerTasks { get; }
    IRepository<Attendance> Attendances { get; }
    IRepository<Hotel> Hotels { get; }
    IRepository<Building> Buildings { get; }
    IRepository<Floor> Floors { get; }
    IRepository<Room> Rooms { get; }
    IRepository<Bus> Buses { get; }
    IRepository<Karwan> Karwans { get; }
    IRepository<GpsLocation> GpsLocations { get; }
    IRepository<Majalis> Majalis { get; }
    IRepository<MajalisProgram> MajalisPrograms { get; }
    IRepository<NamazTiming> NamazTimings { get; }
    IRepository<FoodSchedule> FoodSchedules { get; }
    IRepository<Notification> Notifications { get; }
    IRepository<FileAttachment> FileAttachments { get; }
    IRepository<FamilyMember> FamilyMembers { get; }
    IRepository<Notice> Notices { get; }
    IRepository<TourDocument> TourDocuments { get; }
    IRepository<Zone> Zones { get; }
    IRepository<Scholar> Scholars { get; }
    IRepository<VolunteerPilgrimAssignment> VolunteerPilgrimAssignments { get; }
    IRepository<AuditLog> AuditLogs { get; }
    IRepository<PilgrimRoomHistory> PilgrimRoomHistories { get; }
    IRepository<FinanceExpense> FinanceExpenses { get; }
    IRepository<FinanceBudget> FinanceBudgets { get; }
    IRepository<ExchangeRate> ExchangeRates { get; }
    IRepository<StaffPayment> StaffPayments { get; }
    IRepository<TravelTicket>    TravelTickets    { get; }
    IRepository<IncomingPayment> IncomingPayments { get; }
    IRepository<VendorContract>  VendorContracts  { get; }
    IRepository<MoneyTransfer>    MoneyTransfers    { get; }
    IRepository<AirportTransfer>  AirportTransfers  { get; }
    IRepository<Tour>             Tours             { get; }
    IRepository<TourPackage>      TourPackages      { get; }
    IRepository<TourDaySchedule>  TourDaySchedules  { get; }
    IRepository<PilgrimFeedback>  PilgrimFeedbacks  { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
