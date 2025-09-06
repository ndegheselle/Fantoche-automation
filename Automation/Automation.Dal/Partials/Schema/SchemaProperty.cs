namespace Automation.Models.Schema
{
    public abstract partial class SchemaProperty
    {
        public SchemaProperty(string name)
        {
            Name = name;
        }
    }

    public partial class SchemaValueProperty : SchemaProperty
    {
        public SchemaValueProperty(string name, ISchemaValue element) : base(name)
        {
            Element = element;
        }
    }

    public partial class SchemaObjectProperty : SchemaProperty
    {
        public SchemaObjectProperty(string name, SchemaObject element) : base(name)
        {
            Name = name;
            Element = element;
        }
    }
}