namespace Automation.Shared.Data
{
    public interface IScheduledTask : IIdentifier
    {
        Guid TaskId { get; set; }
        int? Year { get; set; }
        int? Month { get; set; }
        int? Day { get; set; }
        int? Hour { get; set; }
        int? Minute { get; set; }
        int? DayOfWeek { get; set; }
    }
}
