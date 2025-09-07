using System.Collections.ObjectModel;

namespace Automation.Models.Schema
{
    public interface ISchemaObjectProperty
    {
        public string Name { get; }
        public ISchemaElement Element { get; }
    }

    public class SchemaObjectProperty<TElement> : ISchemaObjectProperty where TElement : ISchemaElement
    {
        public string Name { get; private set; } = "";
        public TElement Element { get; private set; }
        ISchemaElement ISchemaObjectProperty.Element => Element;

        public SchemaObjectProperty(string name, TElement element)
        {
            Name = name;
            Element=element;
        }
    }

    public class SchemaPropertyValue : SchemaObjectProperty<SchemaValue>
    {
        public SchemaPropertyValue(string name, SchemaValue element) : base(name, element)
        {}
    }

    public class SchemaPropertyTypedValue : SchemaObjectProperty<SchemaTypedValue>
    {
        public SchemaPropertyTypedValue(string name, SchemaTypedValue element) : base(name, element)
        { }
    }

    public class SchemaPropertyArray : SchemaObjectProperty<SchemaArray>
    {
        public SchemaPropertyArray(string name, SchemaArray element) : base(name, element)
        {}
    }

    public class SchemaPropertyObject : SchemaObjectProperty<SchemaObject>
    {
        public SchemaPropertyObject(string name, SchemaObject element) : base(name, element)
        {}
    }

    public partial class SchemaObject : ISchemaElement
    {
        public ObservableCollection<ISchemaObjectProperty> Properties { get; private set; } = [];
        public ISchemaObjectProperty? this[string name] => Properties.FirstOrDefault(x => x.Name == name);

        public SchemaObject(IEnumerable<ISchemaObjectProperty> properties)
        {
            Properties= new ObservableCollection<ISchemaObjectProperty>(properties);
        }
    }

    public class SchemaArray : ISchemaElement
    {
        public EnumDataType DataType { get; set; }
        public ObservableCollection<SchemaPropertyValue> Values { get; private set; } = [];

        public SchemaArray(ISchemaElement schema)
        {
            Schema=schema;
        }
    }

    public class SchemaTable : ISchemaElement
    {
        public ISchemaElement Schema { get; set; }
        public ObservableCollection<ISchemaObjectProperty> Values { get; private set; } = [];

        public SchemaArray(ISchemaElement schema)
        {
            Schema=schema;
        }
    }
}