using Misbahuda.Domain.Common;
using Misbahuda.Domain.Enums;

namespace Misbahuda.Domain.Entities;

public class TravelTicket : BaseEntity
{
    public string       PassengerName  { get; set; } = string.Empty;
    public StaffRole    PassengerRole  { get; set; }
    public string?      Phone          { get; set; }
    public TicketType   TicketType     { get; set; }
    public string       FromCity       { get; set; } = string.Empty;
    public string       ToCity         { get; set; } = string.Empty;
    public DateTime     TravelDate     { get; set; }
    public string?      Airline        { get; set; }   // airline / bus company
    public string?      TicketNumber   { get; set; }
    public string?      FlightNumber   { get; set; }
    public decimal      Cost           { get; set; }
    public string       Currency       { get; set; } = "USD";
    public TicketStatus Status         { get; set; } = TicketStatus.Pending;
    public string?      Notes          { get; set; }
    public Guid         CreatedById    { get; set; }
}
