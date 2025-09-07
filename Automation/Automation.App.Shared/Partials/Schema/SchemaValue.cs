using Usuel.Shared;

namespace Automation.Models.Schema
{
    public partial class SchemaArray : ISchemaValue
    {
        public ICustomCommand AddCommand { get; }
        public ICustomCommand RemoveCommand { get; }

        public SchemaArray(EnumDataType datatype) {
        
            AddCommand = new DelegateCommand(Add);
            RemoveCommand = new DelegateCommand<dynamic>(Remove);
            DataType = datatype;
        }

        public void Add()
        {
            // TODO : handle default value for each datatype
            Values.Add("");
        }

        public void Remove(dynamic value) { Values.Remove(value); }
    }
}
