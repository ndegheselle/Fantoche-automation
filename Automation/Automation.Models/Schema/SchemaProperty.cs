using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace Automation.Models.Schema
{
    public enum EnumDataType
    {
        String,
        Decimal,
        Boolean,
        DateTime,
        TimeSpan
    }

    public enum EnumPropertyKind
    {
        Value,
        Array,
        Object,
        Table
    }

    [JsonDerivedType(typeof(SchemaValue), "value")]
    [JsonDerivedType(typeof(SchemaArray), "array")]
    [JsonDerivedType(typeof(SchemaObject), "object")]
    [JsonDerivedType(typeof(SchemaTable), "table")]
    public partial class SchemaProperty
    {
        public partial string Name { get; set; }
        public override string ToString() => Name;
    }

    public partial class SchemaObject : SchemaProperty
    {
        public ObservableCollection<SchemaProperty> Properties { get; private set; } = [];
    }

    public partial class SchemaTable : SchemaObject
    {
        public ObservableCollection<dynamic> Values { get; private set; } = [];
    }

    public partial class SchemaValue : SchemaProperty
    {
        public EnumDataType DataType { get; set; }
        public dynamic? Value { get; set; } = null;
    }

    public partial class SchemaArray : SchemaProperty
    {
        public EnumDataType DataType { get; set; }
        public ObservableCollection<dynamic> Values { get; set; } = [];
    }
}
