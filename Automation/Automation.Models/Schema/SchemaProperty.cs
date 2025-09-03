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

    public interface ISchemaElement
    {}

    public interface ISchemaValue : ISchemaElement
    {
        public EnumDataType DataType { get; }
    }

    public partial class SchemaValue : ISchemaValue
    {
        public EnumDataType DataType { get; set; }
        public dynamic? Value { get; set; } = null;
    }

    public partial class SchemaArray : ISchemaValue
    {
        public EnumDataType DataType { get; set; }
        public ObservableCollection<dynamic> Values { get; set; } = [];
    }

    public interface ISchemaProperty
    {
        public string Name { get; }
    }

    public class SchemaValueProperty : ISchemaProperty
    {
        public string Name { get; set; }
        public ISchemaValue Element { get; set; }

        public SchemaValueProperty(string name, ISchemaValue element)
        {
            Name = name;
            Element = element;
        }
    }

    public class SchemaObjectProperty : ISchemaProperty
    {
        public string Name { get; set; }
        public SchemaObject Element { get; set; }

        public SchemaObjectProperty(string name, SchemaObject element)
        {
            Name = name;
            Element = element;
        }
    }

    public partial class SchemaObject : ISchemaElement
    {
        public ObservableCollection<ISchemaProperty> Properties { get; private set; } = [];
    }

    public partial class SchemaTable : ISchemaElement
    {
        public ObservableCollection<ISchemaProperty> Properties { get; private set; } = [];
        public ObservableCollection<ISchemaElement> Values { get; private set; } = [];
    }
}