using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace Automation.Models.Schema
{
    public enum EnumDataType
    {
        Dynamic = 0,
        String,
        Decimal,
        Boolean,
        DateTime,
        TimeSpan
    }


    [JsonDerivedType(typeof(SchemaValue))]
    [JsonDerivedType(typeof(SchemaArray))]
    [JsonDerivedType(typeof(SchemaObject))]
    [JsonDerivedType(typeof(SchemaTable))]
    public interface ISchemaElement
    { }

    public interface ISchemaValue : ISchemaElement
    {
        public EnumDataType DataType { get; }
    }

    public class SchemaValue : ISchemaValue
    {
        public EnumDataType DataType { get; set; }
        public dynamic? Value { get; set; } = null;

        public SchemaValue(EnumDataType dataType)
        {
            DataType = dataType;
        }
    }

    public class SchemaArray : ISchemaValue
    {
        public EnumDataType DataType { get; set; }
        public ObservableCollection<dynamic> Values { get; set; } = [];

        public SchemaArray(EnumDataType dataType)
        {
            DataType = dataType;
        }
    }
}