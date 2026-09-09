namespace Automation.App.Common
{
    public static class Durations
    {
        /// <summary>
        /// A duration as the app shows it : precise on what runs in the blink of an eye, rounded on
        /// what doesn't. Counted in hours rather than in days, a task running for a day still
        /// reading as the hours it took.
        /// </summary>
        public static string Format(TimeSpan duration)
        {
            if (duration.TotalSeconds < 1)
                return $"{duration.TotalMilliseconds:0} ms";
            if (duration.TotalMinutes < 1)
                return $"{duration.TotalSeconds:0.0} s";
            return $"{(int)duration.TotalHours:00}:{duration.Minutes:00}:{duration.Seconds:00}";
        }
    }
}
