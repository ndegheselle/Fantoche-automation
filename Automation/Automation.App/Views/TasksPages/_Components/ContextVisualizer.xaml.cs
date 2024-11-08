using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;

namespace Automation.App.Views.TasksPages.Components
{
    public class ContextValue
    {
        public Dictionary<string, string> OriginContext { get; set; } = [];
        public string DictionaryKey { get; set; } = string.Empty;
        public string Key
        {
            get => DictionaryKey;
            set
            {
                OriginContext[value] = OriginContext[DictionaryKey];
                OriginContext.Remove(DictionaryKey);
                DictionaryKey = value;
            }
        }
        public string Value
        {
            get => OriginContext[DictionaryKey];
            set => OriginContext[DictionaryKey] = value;
        }
    }

    /// <summary>
    /// Logique d'interaction pour ContextVisualizer.xaml
    /// </summary>
    public partial class ContextVisualizer : UserControl
    {
        // Dependency porperty Dictionary<string, string> Context

        public static readonly DependencyProperty ContextProperty = DependencyProperty.Register(
            nameof(Context),
            typeof(Dictionary<string, string>),
            typeof(ContextVisualizer),
            new PropertyMetadata(null, (o, e) => ((ContextVisualizer)o).OnContextChanged()));

        public Dictionary<string, string> Context
        {
            get { return (Dictionary<string, string>)GetValue(ContextProperty); }
            set { SetValue(ContextProperty, value); }
        }

        public void OnContextChanged()
        {
            ContextList.Clear();
            foreach (var pair in Context)
            {
                ContextList.Add(new ContextValue()
                {
                    DictionaryKey = pair.Key,
                    OriginContext = Context
                });
            }

            ContextList.CollectionChanged += (o, e) =>
            {
                if (e.Action == NotifyCollectionChangedAction.Remove && e.OldItems != null)
                {
                    foreach (ContextValue item in e.OldItems)
                    {
                        Context.Remove(item.DictionaryKey);
                    }
                }
            };
        }

        public ObservableCollection<ContextValue> ContextList { get; set; } = [];

        public ContextVisualizer() { InitializeComponent(); }

        private void DataGrid_AddingNewItem(object sender, AddingNewItemEventArgs e)
        {
            // Get available key
            int i = 0;
            string key = $"Key{i}";
            while (Context.ContainsKey(key))
            {
                i++;
                key = $"Key{i}";
            }

            // Add new item
            Context.Add(key, "");
            e.NewItem = new ContextValue() { OriginContext = Context, DictionaryKey = key };
        }
    }
}
