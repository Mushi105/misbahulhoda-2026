namespace Misbahuda.Domain.Enums;

public enum NotificationType
{
    Push = 1,
    WhatsApp = 2,
    Email = 3,
    Sms = 4
}

public enum NotificationEvent
{
    Approval = 1,
    RoomAllocation = 2,
    BusDeparture = 3,
    MajalisReminder = 4,
    FoodTiming = 5,
    EmergencyAlert = 6,
    General = 7
}
