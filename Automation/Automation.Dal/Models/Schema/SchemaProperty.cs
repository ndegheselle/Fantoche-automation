namespace Automation.Dal.Models.Schema
{
    public partial class SchemaProperty
    {
        private string _name = "";
        public partial string Name
        {
            get => _name; set => _name = value;
        }

        private EnumValueType _type;
        public partial EnumValueType Type
        {
            get => _type; set => _type = value;
        }
    }
}