// MIGRATION SHIM: ported from Automation.App.Converters to Avalonia.
// Namespace preserved so migrated XAML keeps referencing Automation.App.Converters.
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace Automation.App.Converters
{
    public class EnumToBooleanConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is Enum e && parameter is Enum p)
                return e.HasFlag(p);
            return false;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value?.Equals(true) == true ? parameter : BindingOperations.DoNothing;
    }

    /// <summary>
    /// Convert an enum to a list of values or descriptions if the enum has a Description attribute.
    /// </summary>
    public class EnumToListConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null)
                return null;

            var enumType = value.GetType();
            if (!enumType.IsEnum)
                throw new ArgumentException("value must be an enumerated type");

            Array values = Enum.GetValues(enumType);
            var list = new List<EnumItem>();

            foreach (var item in values)
            {
                string? name = Enum.GetName(enumType, item);
                if (item == null || name == null)
                    break;

                FieldInfo? field = enumType.GetField(name);
                DescriptionAttribute? attr = field == null
                    ? null
                    : Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) as DescriptionAttribute;
                list.Add(new EnumItem { Value = (Enum)item, Name = attr?.Description ?? name });
            }

            return list;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public struct EnumItem
    {
        public Enum Value { get; set; }
        public string Name { get; set; }
    }
}
