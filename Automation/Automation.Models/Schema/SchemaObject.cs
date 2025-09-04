using System.Collections.ObjectModel;

namespace Automation.Models.Schema
{

    public abstract class SchemaProperty
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
        public ISchemaObject Element { get; set; }
        public SchemaObjectProperty(string name, ISchemaObject element)
        {
            Name = name;
            Element = element;
        }
    }

    public interface ISchemaObject : ISchemaElement
    {
        public ObservableCollection<SchemaProperty> Properties { get; }
    }

    public class SchemaObject : ISchemaObject
    {
        public ObservableCollection<SchemaProperty> Properties { get; private set; }
        public SchemaObject(IEnumerable<SchemaProperty> properties)
        {
            Properties = new ObservableCollection<SchemaProperty>(properties);
        }

        public SchemaProperty? this[string name] => Properties.FirstOrDefault(x => x.Name == name);
    }

    public class SchemaTable : ISchemaObject
    {
        public ObservableCollection<SchemaProperty> Properties { get; private set; }
        public ObservableCollection<ISchemaElement> Values { get; private set; } = [];

        public SchemaTable(IEnumerable<SchemaProperty> properties)
        {
            Properties = new ObservableCollection<SchemaProperty>(properties);
        }
    }
}