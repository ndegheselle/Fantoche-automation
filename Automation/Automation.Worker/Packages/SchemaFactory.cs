using Automation.Models.Schema;
using System.Reflection;
using System.Runtime.Serialization;

namespace Automation.Worker.Packages
{
    public class SchemaDefinitionException : Exception
    {
        public SchemaDefinitionException(string message) : base(message)
        { }
    }

    public static class SchemaExtensions
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

        public static bool IsIgnorable(this PropertyInfo property)
        {
            IEnumerable<IgnoreDataMemberAttribute> ignoreAttribute = property.GetCustomAttributes(false).OfType<IgnoreDataMemberAttribute>();
            return ignoreAttribute.Any();
        }
    }

    public static class SchemaFactory
    {
        public static SchemaValueProperty Convert(Type type, string name = "Root")
        {
            // Simple value
            EnumDataType? dataType = type.ToDataType();
            if (dataType != null)
            {
                return new SchemaValue(name, dataType.Value);
            }

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
                    return ConvertTable(enumerableType, name);
                }
            }

            return ConvertObject(type, name);
        }

        public static SchemaObject ConvertObject(Type type, string name)
        {
            var schemaObject = new SchemaObject(name);
            PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var property in properties)
            {
                if (property.IsIgnorable())
                    continue;

                var schemaProperty = Convert(property.PropertyType, property.Name);
                schemaObject.Properties.Add(schemaProperty);
            }
            return schemaObject;
        }

        public static SchemaTable ConvertTable(Type type, string name)
        {
            List<SchemaColumn> columns = new List<SchemaColumn>();
            PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach(var property in properties)
            {
                if (property.IsIgnorable())
                    continue;

                EnumDataType dataType = property.PropertyType.ToDataType() 
                    ?? throw new Packages.SchemaDefinitionException($"The array '{name}' can't have a complexe typ'.{property.Name}'");
                columns.Add(new SchemaColumn(property.Name, dataType));
            }

            return new SchemaTable(name, columns);
        }
    }
}
