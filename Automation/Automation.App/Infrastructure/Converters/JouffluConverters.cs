// MIGRATION SHIM: ported from Joufflu.Shared.Converters to Avalonia.
// Namespace preserved so migrated XAML/code keeps referencing Joufflu.Shared.Converters.
// Avalonia notes: IValueConverter lives in Avalonia.Data.Converters; there is no WPF
// Visibility (controls use the bool IsVisible), so VisibilityConverter now returns a bool.
using System.Collections;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Joufflu.Shared.Converters
{
    /// <summary>
    /// Convert any value to a boolean.
    /// </summary>
    public class BooleanConverter : IValueConverter
    {
        /// <param name="parameter">bool, indicates whether the result should be true or false</param>
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            bool result;

            if (value is bool b)
                result = b;
            else if (value is string s)
                result = !string.IsNullOrEmpty(s);
            else if (value is int i)
                result = i > 0;
            else if (value is ICollection collection)
                result = collection.Count > 0;
            else
                result = value != null;

            // e.g. if the parameter is "false" and the value is null, the result will be true.
            if (!bool.TryParse(parameter?.ToString(), out var target))
                target = true;

            return result == target;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    /// <summary>
    /// Combines a list of values into a single boolean (And / Or operator via parameter).
    /// </summary>
    public class BooleansConverter : IMultiValueConverter
    {
        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            string conjunction = parameter?.ToString()?.Trim() ?? "&&";
            var converter = new BooleanConverter();
            bool result = conjunction == "&&";

            foreach (object? value in values)
            {
                bool b = (bool)converter.Convert(value, targetType, parameter, culture)!;
                result = conjunction == "&&" ? result && b : result || b;
            }
            return result;
        }
    }

    public class BooleanFlipConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool booleanValue)
                return !booleanValue;
            return value;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool booleanValue)
                return !booleanValue;
            return value;
        }
    }

    /// <summary>
    /// MIGRATION: WPF returned a Visibility; Avalonia controls use the bool IsVisible, so this
    /// now returns a bool. When porting XAML, bind IsVisible (not Visibility) to this converter.
    /// </summary>
    public class VisibilityConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var boolConverter = new BooleanConverter();
            return boolConverter.Convert(value, targetType, parameter, culture) as bool? ?? false;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
