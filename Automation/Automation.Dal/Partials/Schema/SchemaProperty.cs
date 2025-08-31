namespace Automation.Models.Schema
{
    public partial class SchemaProperty
    {
        private string _name = "";
        public partial string Name
        {
            get => _name; set => _name = value;
        }
    }
}