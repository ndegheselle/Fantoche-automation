using System.Windows.Data;

namespace Automation.App.Converters
{
    /// <summary>
    /// Convert an enum to a list of values or descriptions if the enum has a Description attribute
    /// </summary>
    public class EnumToListConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
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

                System.Reflection.FieldInfo? field = enumType.GetField(name);
                System.ComponentModel.DescriptionAttribute? attr = null;
                if (field != null)
                    attr = Attribute.GetCustomAttribute(field, typeof(System.ComponentModel.DescriptionAttribute)) as System.ComponentModel.DescriptionAttribute;
                list.Add(new EnumItem() { Value = (Enum)item, Name = attr?.Description ?? name });
            }

            return list;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public struct EnumItem
    {
        public Enum Value { get; set; }
        public string Name { get; set; }
    }
}
