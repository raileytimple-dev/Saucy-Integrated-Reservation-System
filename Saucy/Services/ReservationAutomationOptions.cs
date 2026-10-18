namespace SaucyMVC.Services;

public sealed class ReservationAutomationOptions
{
    public const string SectionName = "ReservationAutomation";

    public int PendingHoldMinutes { get; set; } = 15;
    public int NoShowGraceMinutes { get; set; } = 15;
    public int WalkInTurnoverBufferMinutes { get; set; } = 30;
    public int DefaultDiningMinutes { get; set; } = 120;
}
