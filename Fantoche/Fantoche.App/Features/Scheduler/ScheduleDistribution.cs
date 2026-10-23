using Fantoche.Shared.Data;

namespace Fantoche.App.Features.Scheduler
{
    /// <summary>The runs the schedules start within an hour of a day.</summary>
    /// <param name="Intensity">From 0 (no run) to 1 (the busiest hour), what the cell is colored with.</param>
    public record DistributionCell(int Hour, int Count, double Intensity, string ToolTip)
    {
        public string CountText => Count == 0 ? "" : $"{Count}";
    }

    public record DistributionDay(DateTime Date, List<DistributionCell> Hours);

    /// <summary>
    /// How the runs of the schedules spread over the next days, hour by hour : where they pile up is
    /// where the workers will be busy.
    /// </summary>
    public static class ScheduleDistribution
    {
        /// <summary>An hour with a single run still reads as one, next to a busy one.</summary>
        private const double LowestIntensity = 0.15;

        /// <param name="schedules">Each schedule with the name of the element it starts.</param>
        /// <param name="from">The first day laid out, from its midnight.</param>
        public static List<DistributionDay> Build(IEnumerable<(string Name, Schedule Schedule)> schedules, DateTime from, int days)
        {
            DateTime start = from.Date;
            DateTime end = start.AddDays(days);

            var runs = schedules
                .SelectMany(x => x.Schedule.OccurrencesBetween(start, end).Select(at => (x.Name, At: at)))
                .ToList();
            var byHour = runs
                .GroupBy(x => x.At.Date.AddHours(x.At.Hour))
                .ToDictionary(x => x.Key, x => x.Select(run => run.Name).ToList());
            int busiest = byHour.Count == 0 ? 1 : byHour.Values.Max(x => x.Count);

            return [.. Enumerable.Range(0, days).Select(day =>
            {
                DateTime date = start.AddDays(day);
                List<DistributionCell> hours = [.. Enumerable.Range(0, 24).Select(hour =>
                {
                    DateTime slot = date.AddHours(hour);
                    List<string> names = byHour.GetValueOrDefault(slot) ?? [];
                    return CellOf(slot, names, busiest);
                })];
                return new DistributionDay(date, hours);
            })];
        }

        private static DistributionCell CellOf(DateTime slot, List<string> names, int busiest)
        {
            string label = $"{slot:ddd dd MMM HH}:00";
            if (names.Count == 0)
                return new DistributionCell(slot.Hour, 0, 0, $"{label} : no run");

            double intensity = LowestIntensity + (1 - LowestIntensity) * names.Count / busiest;
            IEnumerable<string> elements = names
                .GroupBy(x => x)
                .OrderByDescending(x => x.Count())
                .Select(x => x.Count() == 1 ? x.Key : $"{x.Key} ×{x.Count()}");
            string toolTip = $"{label} : {names.Count} run{(names.Count == 1 ? "" : "s")}\n{string.Join("\n", elements)}";
            return new DistributionCell(slot.Hour, names.Count, intensity, toolTip);
        }
    }
}
