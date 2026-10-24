using Cronos;

namespace Fantoche.Shared.Data
{
    /// <summary>
    /// When a task or workflow is started on its own, and with what. Stored with the element it
    /// starts (see <see cref="Scoped.BaseAutomationTask.Schedules"/>).
    /// <para>
    /// The cron is a standard one, "minute hour day-of-month month day-of-week" (e.g. "0 6 * * 1-5"
    /// for 6:00 on the working days), read in the local time zone : the one of the supervisor
    /// starting it (see <c>Fantoche.Services.Local.Scheduler</c>), set with TZ in a container.
    /// </para>
    /// </summary>
    public class Schedule
    {
        public string Cron { get; set; } = "";

        /// <summary>
        /// The parameters each execution starts with, checked against the
        /// <see cref="Scoped.BaseAutomationTask.InputSchema"/> of the element. Null when it expects none.
        /// </summary>
        public string? ParametersJson { get; set; }

        /// <summary>Why [cron] isn't a cron a schedule can hold, null when it is one.</summary>
        public static string? Validate(string cron)
        {
            try
            {
                CronExpression.Parse(cron, CronFormat.Standard);
                return null;
            }
            catch (CronFormatException exception)
            {
                return exception.Message;
            }
        }

        /// <summary>
        /// When the schedule starts its element from [from], included, to [to], excluded. Empty when
        /// its cron can't be read.
        /// </summary>
        public IEnumerable<DateTime> OccurrencesBetween(DateTime from, DateTime to)
        {
            if (Validate(Cron) != null)
                return [];

            CronExpression cron = CronExpression.Parse(Cron, CronFormat.Standard);
            return cron
                .GetOccurrences(from.ToUniversalTime(), to.ToUniversalTime(), TimeZoneInfo.Local)
                .Select(x => x.ToLocalTime());
        }

        /// <summary>The next time the schedule starts its element after [from], null when never.</summary>
        public DateTime? NextOccurrence(DateTime from)
        {
            if (Validate(Cron) != null)
                return null;

            CronExpression cron = CronExpression.Parse(Cron, CronFormat.Standard);
            return cron.GetNextOccurrence(from.ToUniversalTime(), TimeZoneInfo.Local)?.ToLocalTime();
        }
    }
}
