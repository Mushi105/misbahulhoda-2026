using Microsoft.EntityFrameworkCore;
using Misbahuda.Application.Interfaces;
using Misbahuda.Domain.Entities;

namespace Misbahuda.Infrastructure.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ICurrentUserService currentUser)
    : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Pilgrim> Pilgrims => Set<Pilgrim>();
    public DbSet<Volunteer> Volunteers => Set<Volunteer>();
    public DbSet<VolunteerTask> VolunteerTasks => Set<VolunteerTask>();
    public DbSet<Attendance> Attendances => Set<Attendance>();
    public DbSet<Hotel> Hotels => Set<Hotel>();
    public DbSet<Building> Buildings => Set<Building>();
    public DbSet<Floor> Floors => Set<Floor>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Bus> Buses => Set<Bus>();
    public DbSet<Karwan> Karwans => Set<Karwan>();
    public DbSet<GpsLocation> GpsLocations => Set<GpsLocation>();
    public DbSet<Majalis> Majalis => Set<Majalis>();
    public DbSet<MajalisProgram> MajalisPrograms => Set<MajalisProgram>();
    public DbSet<NamazTiming> NamazTimings => Set<NamazTiming>();
    public DbSet<FoodSchedule> FoodSchedules => Set<FoodSchedule>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<FileAttachment> FileAttachments => Set<FileAttachment>();
    public DbSet<FamilyMember> FamilyMembers => Set<FamilyMember>();
    public DbSet<Notice> Notices => Set<Notice>();
    public DbSet<TourDocument> TourDocuments => Set<TourDocument>();
    public DbSet<FinanceExpense> FinanceExpenses => Set<FinanceExpense>();
    public DbSet<FinanceBudget> FinanceBudgets => Set<FinanceBudget>();
    public DbSet<ExchangeRate> ExchangeRates => Set<ExchangeRate>();
    public DbSet<StaffPayment> StaffPayments => Set<StaffPayment>();
    public DbSet<TravelTicket>    TravelTickets    => Set<TravelTicket>();
    public DbSet<IncomingPayment> IncomingPayments => Set<IncomingPayment>();
    public DbSet<VendorContract>  VendorContracts  => Set<VendorContract>();
    public DbSet<MoneyTransfer>    MoneyTransfers    => Set<MoneyTransfer>();
    public DbSet<AirportTransfer>  AirportTransfers  => Set<AirportTransfer>();
    public DbSet<Tour>             Tours             => Set<Tour>();
    public DbSet<TourPackage>      TourPackages      => Set<TourPackage>();
    public DbSet<TourDaySchedule>  TourDaySchedules  => Set<TourDaySchedule>();
    public DbSet<PilgrimFeedback>  PilgrimFeedbacks  => Set<PilgrimFeedback>();
    public DbSet<Zone> Zones => Set<Zone>();
    public DbSet<Scholar> Scholars => Set<Scholar>();
    public DbSet<VolunteerPilgrimAssignment> VolunteerPilgrimAssignments => Set<VolunteerPilgrimAssignment>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<PilgrimRoomHistory> PilgrimRoomHistories => Set<PilgrimRoomHistory>();
    public DbSet<DeviceToken> DeviceTokens => Set<DeviceToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (entityType.ClrType.IsAssignableTo(typeof(Domain.Common.BaseEntity)))
            {
                modelBuilder.Entity(entityType.ClrType)
                    .HasQueryFilter(BuildSoftDeleteFilter(entityType.ClrType));
            }
        }
    }

    private static System.Linq.Expressions.LambdaExpression BuildSoftDeleteFilter(Type type)
    {
        var param = System.Linq.Expressions.Expression.Parameter(type, "e");
        var prop = System.Linq.Expressions.Expression.Property(param, "IsDeleted");
        var condition = System.Linq.Expressions.Expression.Equal(prop, System.Linq.Expressions.Expression.Constant(false));
        return System.Linq.Expressions.Expression.Lambda(condition, param);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var userId = currentUser.UserId?.ToString();

        foreach (var entry in ChangeTracker.Entries<Domain.Common.BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.CreatedBy = userId;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.UpdatedBy = userId;
                    break;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
