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

    public interface ISchemaElement
    {}

    public class SchemaValue : ISchemaElement
    {
        public string ContextReference { get; set; } = "";
        public dynamic? Value { get; set; } = null;
    }

    public class SchemaTypedValue : SchemaValue
    {
        public EnumDataType DataType { get; set; }
        public SchemaTypedValue(EnumDataType dataType)
        {
            DataType=dataType;
        }
    }
}