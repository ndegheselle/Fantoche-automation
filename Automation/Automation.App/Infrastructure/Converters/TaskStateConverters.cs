using System.Globalization;
using Automation.Shared.Data.Execution;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Lucide.Avalonia;

namespace Automation.App.Infrastructure.Converters
{
    /// <summary>Maps a task instance <see cref="EnumTaskState"/> to a Lucide icon kind.</summary>
    public class TaskStateToIconConverter : IValueConverter
    {
        public static readonly TaskStateToIconConverter Instance = new();

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is EnumTaskState state
                ? state switch
                {
                    EnumTaskState.Pending => LucideIconKind.Clock,
                    EnumTaskState.Waiting => LucideIconKind.Hourglass,
                    EnumTaskState.Progressing => LucideIconKind.LoaderCircle,
                    EnumTaskState.Completed => LucideIconKind.CircleCheck,
                    EnumTaskState.Failed => LucideIconKind.CircleX,
                    EnumTaskState.Canceled => LucideIconKind.CirclePause,
                    _ => LucideIconKind.Circle,
                }
                : LucideIconKind.Circle;

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    /// <summary>Maps a task instance <see cref="EnumTaskState"/> to a status brush.</summary>
    public class TaskStateToBrushConverter : IValueConverter
    {
        public static readonly TaskStateToBrushConverter Instance = new();

        private static readonly IBrush Warning = new SolidColorBrush(Color.Parse("#EE9B00"));
        private static readonly IBrush Success = new SolidColorBrush(Color.Parse("#2E9E5B"));
        private static readonly IBrush Error = new SolidColorBrush(Color.Parse("#D64550"));
        private static readonly IBrush Muted = new SolidColorBrush(Color.Parse("#8D99AE"));

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is EnumTaskState state
                ? state switch
                {
                    EnumTaskState.Pending or EnumTaskState.Waiting => Warning,
                    EnumTaskState.Completed => Success,
                    EnumTaskState.Failed => Error,
                    EnumTaskState.Canceled => Muted,
                    _ => Brushes.Gray,
                }
                : Brushes.Gray;

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
