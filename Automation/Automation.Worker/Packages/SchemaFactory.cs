using Automation.Models.Schema;
using System;
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
                    return new SchemaTable();
                }
            }

            return new SchemaObject();
        }

        public static IEnumerable<ISchemaProperty> ConvertProperties(Type type)
        {
            IEnumerable<PropertyInfo> properties = type
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(prop => prop.IsIgnorable() == false);
            foreach (var property in properties)
            {
                if (property.PropertyType.IsValue(out EnumDataType datatype))
                {
                    new SchemaValueProperty(property.PropertyType);
                }
                else
                {
                    var subObject = new SchemaObject(ConvertProperties(property.PropertyType));
                    new SchemaObjectProperty(property.Name, subObject);
                }

                    var schemaProperty = Convert(, );
                schemaObject.Properties.Add(schemaProperty);
            }
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
            foreach (var property in properties)
            {
                if (property.IsIgnorable())
                    continue;

                EnumDataType dataType = property.PropertyType.IsValueType()
                    ?? throw new Packages.SchemaDefinitionException($"The array '{name}' can't have a complexe typ'.{property.Name}'");
                columns.Add(new SchemaColumn(property.Name, dataType));
            }

            return new SchemaTable(name, columns);
        }
    }
}
