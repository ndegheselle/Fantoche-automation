using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Automation.App.Converters
{
    /// <summary>
    /// Converts between the hex color string stored on <see cref="Automation.Shared.Data.Scoped.ScopedMetadata.Color"/>
    /// and the <see cref="Color"/> expected by <see cref="Joufflu.Inputs.Controls.ColorPicker"/>, defaulting to white
    /// when no color has been set yet.
    /// </summary>
    public class HexColorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string hex && !string.IsNullOrWhiteSpace(hex))
            {
                try
                {
                    return ColorConverter.ConvertFromString(hex);
                }
                catch (FormatException)
                { }
            }

            return Colors.White;
        }

        public object? ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value is Color color ? color.ToString() : null;
        }
    }
}
