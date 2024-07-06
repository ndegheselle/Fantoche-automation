using System.Collections;
using System.Collections.ObjectModel;
using System.Windows.Controls;

namespace Automation.App.Components
{
    public class DataNode
    {
        public string? Key { get; set; }
    }

    public class  DataValue : DataNode
    {
        public object? Value { get; set; }

        public DataValue()
        {
        }

        public DataValue(object value, string? key = null)
        {
            Key = key;
            Value = value;
        }
    }

    public class  DataTree : DataNode
    {
        public ObservableCollection<DataNode> Childrens { get; set; } = [];
    }

    /// <summary>
    /// Logique d'interaction pour DataVisualizer.xaml
    /// </summary>
    public partial class DataVisualizer : UserControl
    {
        public DataTree DataTree { get; set; }

        public DataVisualizer()
        {

            Dictionary<string, object> dico = new Dictionary<string, object>()
            {
                { "key", "val" },
                { "key2", new List<string>() { "val1", "val2" } },
                { "key3", new Dictionary<string, object>() { { "subkey", "subval" } } }
            };
            DataTree = ConvertToDataNode(dico) as DataTree;
            InitializeComponent();
        }

        public DataNode ConvertToDataNode(object? item, string? key = null)
        {
            if (item is IDictionary dico)
            {
                DataTree tree = new DataTree();
                tree.Key = key;
                tree.Childrens = new ObservableCollection<DataNode>();

                foreach (DictionaryEntry entry in dico)
                {
                    tree.Childrens.Add(ConvertToDataNode(entry.Value, entry.Key.ToString()));
                }

                return tree;
            }
            else if (item is IList enumerable)
            {
                DataTree tree = new DataTree();
                tree.Key = key;
                tree.Childrens = new ObservableCollection<DataNode>();

                foreach (var subitem in enumerable)
                {
                    tree.Childrens.Add(ConvertToDataNode(subitem));
                }

                return tree;
            }
            else
            {
                DataValue value = new DataValue(item);
                value.Key = key;
                return value;
            }
        }
    }
}
