using Fantoche.Shared.Data;

namespace Fantoche.Shared.Tests;

/// <summary>
/// When a schedule starts its element : what the scheduler page lays out, and what the workers
/// will run.
/// </summary>
[TestFixture]
public class ScheduleTests
{
    private static readonly DateTime Monday = new(2026, 10, 26, 0, 0, 0, DateTimeKind.Local);

    [Test]
    public void Working_days_cron_starts_once_on_each_working_day()
    {
        Schedule schedule = new() { Cron = "0 6 * * 1-5" };

        List<DateTime> occurrences = [.. schedule.OccurrencesBetween(Monday, Monday.AddDays(7))];

        Assert.That(occurrences, Is.EqualTo(Enumerable.Range(0, 5).Select(day => Monday.AddDays(day).AddHours(6))));
    }

    [Test]
    public void Next_occurrence_is_the_first_one_after_the_given_time()
    {
        Schedule schedule = new() { Cron = "30 * * * *" };

        Assert.That(schedule.NextOccurrence(Monday.AddMinutes(45)), Is.EqualTo(Monday.AddHours(1).AddMinutes(30)));
    }

    [TestCase("")]
    [TestCase("every day")]
    [TestCase("61 * * * *")]
    [TestCase("0 0 * *")]
    public void Unreadable_cron_is_refused_and_never_starts(string cron)
    {
        Schedule schedule = new() { Cron = cron };

        Assert.Multiple(() =>
        {
            Assert.That(Schedule.Validate(cron), Is.Not.Null);
            Assert.That(schedule.OccurrencesBetween(Monday, Monday.AddDays(7)), Is.Empty);
            Assert.That(schedule.NextOccurrence(Monday), Is.Null);
        });
    }
}
