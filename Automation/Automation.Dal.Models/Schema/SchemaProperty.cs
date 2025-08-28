using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace Automation.Dal.Models.Schema
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
}
