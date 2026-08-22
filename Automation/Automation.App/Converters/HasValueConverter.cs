using System.Globalization;
using System.Windows.Data;

namespace Automation.App.Converters
{
    /// <summary>
    /// True when the bound value is set : not null, and not blank for a string. Made for the triggers
    /// of an optional value, a WPF trigger only being able to test equality.
    /// </summary>
    public class HasValueConverter : IValueConverter
    {
        public static readonly HasValueConverter Default = new();

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value is string text ? !string.IsNullOrWhiteSpace(text) : value != null;
        }

        public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
