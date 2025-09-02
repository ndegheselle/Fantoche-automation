using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;

namespace Automation.Models.Schema
{
    public partial class SchemaProperty
    {
        private string _name = "";
        public partial string Name
        {
            get => _name; set => _name = value;
        }

        private EnumPropertyKind _kind;
        public partial EnumPropertyKind Kind
        {
            get => _kind; private set => _kind = value;
        }

        public SchemaProperty(string name, EnumPropertyKind kind)
        {
            Name = name;
            Kind = kind;
        }
    }

    public partial class SchemaValue : SchemaProperty
    {
        public SchemaValue(string name, EnumDataType type) : base(name, EnumPropertyKind.Value)
        {
            DataType = type;
        }
    }

    public partial class SchemaArray : SchemaProperty
    {
        public SchemaArray(string name, EnumDataType type) : base(name, EnumPropertyKind.Array)
        {
            DataType = type;
        }
    }

    public partial class SchemaObject : SchemaProperty
    {
        public SchemaObject(string name) : base(name, EnumPropertyKind.Object)
        { }
    }

    public partial class SchemaTable : SchemaProperty
    {
        public SchemaTable(string name, List<SchemaColumn> columns) : base(name, EnumPropertyKind.Table)
        {
            Columns = columns;
        }
    }
}