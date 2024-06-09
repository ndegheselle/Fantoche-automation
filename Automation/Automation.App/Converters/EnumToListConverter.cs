using System.Windows.Data;

namespace Automation.App.Converters
{
    /// <summary>
    /// Convert an enum to a list of values or descriptions if the enum has a Description attribute
    /// </summary>
    public class EnumToListConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
                return null;

            var enumType = value.GetType();
            if (!enumType.IsEnum)
                throw new ArgumentException("value must be an enumerated type");

            var values = Enum.GetValues(enumType);
            var list = new List<EnumItem>();

            foreach (var item in values)
            {
                var name = Enum.GetName(enumType, item);
                var description = name;
                var field = enumType.GetField(name);
                var attr = (System.ComponentModel.DescriptionAttribute)Attribute.GetCustomAttribute(field, typeof(System.ComponentModel.DescriptionAttribute));
                if (attr != null)
                    description = attr.Description;
                list.Add(new EnumItem() { Value = (Enum)item, Name = description ?? name });
            }

            return list;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class EnumItem
    {
        public Enum Value { get; set; }
        public string Name { get; set; }
    }
}
