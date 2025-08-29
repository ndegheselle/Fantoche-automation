using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace Automation.Models.Schema
{
    public enum EnumValueType
    {
        Dynamic,
        String,
        Decimal,
        Boolean,
        DateTime,
        TimeSpan,
        Object
    }

    [JsonDerivedType(typeof(SchemaObject), "object")]
    [JsonDerivedType(typeof(SchemaValue), "value")]
    [JsonDerivedType(typeof(SchemaValue), "array")]
    [JsonDerivedType(typeof(SchemaTable), "table")]
    public partial class SchemaProperty
    {
        public bool IsArray { get; set; }
        public partial string Name { get; set; }
        public partial EnumValueType Type { get; set; }

        public override string ToString() => Name;
    }

    public partial class SchemaObject : SchemaProperty
    {
        public ObservableCollection<SchemaProperty> Properties { get; private set; } = [];
    }

    public partial class SchemaValue : SchemaProperty
    {
        public dynamic? Value { get; set; } = null;
    }

    public partial class SchemaArray : SchemaProperty
    {
        public List<dynamic> Value { get; set; } = [];
    }

    public partial class SchemaTable : SchemaObject
    {
        public List<dynamic> Values { get; private set; } = [];
    }
}
