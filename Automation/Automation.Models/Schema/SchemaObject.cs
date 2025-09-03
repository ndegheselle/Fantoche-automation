using System.Collections.ObjectModel;

namespace Automation.Models.Schema
{

    public interface ISchemaProperty
    {
        public string Name { get; }
    }

    public partial class SchemaValueProperty : ISchemaProperty
    {
        public string Name { get; set; }
        public ISchemaValue Element { get; set; }

        public SchemaValueProperty(string name, ISchemaValue element)
        {
            Name = name;
            Element = element;
        }
    }

    public partial class SchemaObjectProperty : ISchemaProperty
    {
        public string Name { get; set; }
        public SchemaObject Element { get; set; }

        public SchemaObjectProperty(string name, SchemaObject element)
        {
            Name = name;
            Element = element;
        }
    }

    public class SchemaObject : ISchemaElement
    {
        public ObservableCollection<ISchemaProperty> Properties { get; private set; }
        public SchemaObject(IEnumerable<ISchemaProperty> properties)
        {
            Properties = new ObservableCollection<ISchemaProperty>(properties);
        }
    }

    public class SchemaTable : ISchemaElement
    {
        public ObservableCollection<ISchemaProperty> Properties { get; private set; } = [];
        public ObservableCollection<ISchemaElement> Values { get; private set; } = [];

        public SchemaTable(IEnumerable<ISchemaProperty> properties)
        {
            Properties = new ObservableCollection<ISchemaProperty>(properties);
        }
    }
}