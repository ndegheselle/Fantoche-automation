using Automation.Models.Schema;
using System.Reflection;

namespace Automation.Dal.Partials.Schema
{
    public static class TypeExtensions
    {
        /// <summary>
        /// Convert type to a <see cref="EnumDataType"/> if the type is compatible.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static EnumDataType? ToDataType(this Type type) => type switch
        {
            _ when type == typeof(string) => EnumDataType.String,
            _ when type == typeof(int) => EnumDataType.Decimal,
            _ when type == typeof(float) => EnumDataType.Decimal,
            _ when type == typeof(double) => EnumDataType.Decimal,
            _ when type == typeof(decimal) => EnumDataType.Decimal,
            _ when type == typeof(bool) => EnumDataType.Boolean,
            _ when type == typeof(DateTime) => EnumDataType.DateTime,
            _ when type == typeof(TimeSpan) => EnumDataType.TimeSpan,
            _ => null
        };

        /// <summary>
        /// If the type is an IEnumerable<> get the generic type, null otherwise.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static Type? GetEnumerableType(this Type type)
        {
            var enumerableInterface = type.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
            return enumerableInterface?.GetGenericArguments()[0];
        }
    }

    public static class SchemaFactory
    {
        public static SchemaProperty Convert(Type type, string name = "Root")
        {
            // Simple value
            EnumDataType? dataType = type.ToDataType();
            if (dataType != null)
            {
                return new SchemaValue(name, dataType.Value);
            }

            // Array and Table
            Type? enumerableType = type.GetEnumerableType();
            if (enumerableType != null)
            {
                EnumDataType? dataTypeEnumerable = enumerableType.ToDataType();
                if (dataTypeEnumerable != null)
                {
                    return new SchemaArray(name, dataTypeEnumerable.Value);
                }
                else
                {
                    return new SchemaTable(name, dataTypeEnumerable.Value);
                }
            }

            // Object
            return ConvertObject(type, name);
        }

        public static SchemaObject ConvertObject(Type type, string name)
        {
            var schemaObject = new SchemaObject(name);
            PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var property in properties)
            {
                var schemaProperty = Convert(property.PropertyType, property.Name);
                schemaObject.Properties.Add(schemaProperty);
            }
            return schemaObject;
        }
    }
}
