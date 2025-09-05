using System.Collections.ObjectModel;

namespace Automation.Models.Schema
{

    public abstract partial class SchemaProperty
    {
        public string Name { get; set; } = "";

        public override bool Equals(object? obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;
            var other = (SchemaObjectProperty)obj;
            return Name == other.Name;
        }

        public override int GetHashCode()
        {
            return Name?.GetHashCode() ?? 0;
        }
    }

    public partial class SchemaValueProperty : SchemaProperty
    {
        public ISchemaValue Element { get; set; }
        public SchemaValueProperty(string name, ISchemaValue element)
        {
            Name = name;
            Element = element;
        }
    }

    public partial class SchemaObjectProperty : SchemaProperty
    {
        public SchemaObject Element { get; set; }
        public SchemaObjectProperty(string name, SchemaObject element)
        {
            Name = name;
            Element = element;
        }
    }

    public partial class SchemaObject : ISchemaElement
    {
        public ObservableCollection<SchemaProperty> Properties { get; private set; }

        public SchemaObject()
        {
            Properties = [];
        }
        public SchemaObject(IEnumerable<SchemaProperty> properties)
        {
            Properties = new ObservableCollection<SchemaProperty>(properties);
        }

        public SchemaProperty? this[string name] => Properties.FirstOrDefault(x => x.Name == name);
    }

    public class SchemaTable : ISchemaElement
    {
        public SchemaObject Schema { get; set; }
        public ObservableCollection<SchemaObject> Values { get; private set; } = [];

        public SchemaTable(SchemaObject schema) { Schema = schema; }
    }
}