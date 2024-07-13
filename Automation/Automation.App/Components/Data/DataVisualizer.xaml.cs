using Automation.App.Base;
using Automation.Shared.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Automation.App.Components.Data
{
    /// <summary>
    /// [Experimental] Show any data in a tree view
    /// Logique d'interaction pour DataVisualizer.xaml
    /// </summary>
    public partial class DataVisualizer : UserControl
    {
        private readonly App _app = (App)App.Current;
        private readonly IModalContainer _modal;
        public DataTree DataTree { get; set; }

        public DataVisualizer()
        {
            _modal = _app.ServiceProvider.GetRequiredService<IModalContainer>();
            Dictionary<string, object> dico = new Dictionary<string, object>()
            {
                { "key", "val" },
                { "key2", new List<string>() { "val1", "val2" } },
                { "key3", new Dictionary<string, string>() { { "subkey", "subval" } } }
            };
            DataTree = (DataTree)ConvertToDataNode("root", dico);
            InitializeComponent();
        }

        public DataNode ConvertToDataNode(string key, object? item)
        {
            if (item is IDictionary dico)
            {
                DataTree tree = new DataTree();
                tree.Type = EnumTreeType.Dictionnary;
                tree.Key = key;
                tree.Childrens.Clear();

                foreach (DictionaryEntry entry in dico)
                {
                    DataNode node = ConvertToDataNode(entry.Key.ToString() ?? "", entry.Value);
                    node.Parent = tree;
                    tree.Childrens.Add(node);
                }

                return tree;
            }
            else if (item is IList enumerable)
            {
                DataTree tree = new DataTree();
                tree.Type = EnumTreeType.List;
                tree.Key = key;
                tree.Childrens.Clear();

                for (int i = 0; i < enumerable.Count; i++)
                {
                    DataNode node = ConvertToDataNode(i.ToString(), enumerable[i]);
                    node.Parent = tree;
                    tree.Childrens.Add(node);
                }

                return tree;
            }
            else
            {
                DataValue value = new DataValue(key, item.ToString());
                return value;
            }
        }
    }
}
