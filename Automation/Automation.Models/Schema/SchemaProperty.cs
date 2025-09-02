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

    // XXX : made things too complexe, instead of using partial just use the complete definition in Automation.App.Shared
    [JsonDerivedType(typeof(SchemaValue), "value")]
    [JsonDerivedType(typeof(SchemaArray), "array")]
    [JsonDerivedType(typeof(SchemaObject), "object")]
    [JsonDerivedType(typeof(SchemaTable), "table")]
    public partial class SchemaProperty
    {
        public partial string Name { get; set; }
        public partial EnumPropertyKind Kind { get; private set; }
        public override string ToString() => Name;
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

    public partial class SchemaObject : SchemaProperty
    {
        public ObservableCollection<SchemaProperty> Properties { get; private set; } = [];
    }

    public partial class SchemaTable : SchemaProperty
    {
        public ObservableCollection<SchemaProperty> Properties { get; private set; } = [];
        public ObservableCollection<SchemaObject> Values { get; private set; } = [];
    }
}
