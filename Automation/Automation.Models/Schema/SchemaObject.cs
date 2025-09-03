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
        public ISchemaObject Element { get; set; }

        public SchemaObjectProperty(string name, ISchemaObject element)
        {
            Name = name;
            Element = element;
        }
    }

    public interface ISchemaObject : ISchemaElement
    {
        public ObservableCollection<ISchemaProperty> Properties { get; set; }
    }

    public class SchemaObject : ISchemaObject
    {
        public ObservableCollection<ISchemaProperty> Properties { get; private set; }
        public SchemaObject(IEnumerable<ISchemaProperty> properties)
        {
            Properties = new ObservableCollection<ISchemaProperty>(properties);
        }
    }

    public class SchemaTable : ISchemaObject
    {
        public ObservableCollection<ISchemaProperty> Properties { get; private set; } = [];
        public ObservableCollection<ISchemaElement> Values { get; private set; } = [];

        public SchemaTable(IEnumerable<ISchemaProperty> properties)
        {
            Properties = new ObservableCollection<ISchemaProperty>(properties);
        }
    }
}