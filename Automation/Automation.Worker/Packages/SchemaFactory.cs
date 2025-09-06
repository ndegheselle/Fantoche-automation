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
        public static bool IsValue(this Type type, out EnumDataType datatype)
        {
            datatype = type switch
            {
                _ when type == typeof(string) => EnumDataType.String,
                _ when type == typeof(int) => EnumDataType.Decimal,
                _ when type == typeof(float) => EnumDataType.Decimal,
                _ when type == typeof(double) => EnumDataType.Decimal,
                _ when type == typeof(decimal) => EnumDataType.Decimal,
                _ when type == typeof(bool) => EnumDataType.Boolean,
                _ when type == typeof(DateTime) => EnumDataType.DateTime,
                _ when type == typeof(TimeSpan) => EnumDataType.TimeSpan,
                _ => EnumDataType.Dynamic
            };
            return datatype != EnumDataType.Dynamic;
        }

        /// <summary>
        /// If the type is an IEnumerable<> get the generic type, null otherwise.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsEnumerable(this Type type, out Type? enumerableType)
        {
            var enumerableInterface = type.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
            enumerableType = enumerableInterface?.GetGenericArguments()[0];
            return enumerableType != null;
        }

        public static bool IsIgnorable(this PropertyInfo property)
        {
            IEnumerable<IgnoreDataMemberAttribute> ignoreAttribute = property.GetCustomAttributes(false).OfType<IgnoreDataMemberAttribute>();
            return ignoreAttribute.Any();
        }
    }

    public static class SchemaFactory
    {
        /// <summary>
        /// Convert a type to a ISchema element recusively.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static ISchemaElement Convert(Type type)
        {
            // Simple value
            if (type.IsValue(out EnumDataType dataType))
            {
                return new SchemaValue(dataType);
            }
            else if (type.IsEnumerable(out Type? enumerableType) && enumerableType != null)
            {
                if (enumerableType.IsValue(out EnumDataType enumerableDataType))
                {
                    return new SchemaArray(enumerableDataType);
                }
                else
                {
                    return new SchemaTable(ConvertProperties(enumerableType));
                }
            }

            return new SchemaObject(ConvertProperties(type));
        }

        /// <summary>
        /// Get a recursive list of properties from a type.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private static IEnumerable<SchemaProperty> ConvertProperties(Type type)
        {
            IEnumerable<PropertyInfo> typeProps = type
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(prop => prop.IsIgnorable() == false);

            List<SchemaProperty> properties = [];
            foreach (var property in typeProps)
            {
                ISchemaElement element = Convert(property.PropertyType);
                
                if (element is ISchemaValue elementValue)
                    properties.Add(new SchemaValueProperty(property.Name, elementValue));
                else if (element is SchemaObject elementObject)
                    properties.Add(new SchemaObjectProperty(property.Name, elementObject));
            }
            return properties;
        }
    }
}
