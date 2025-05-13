using Joufflu.Popups;
using Joufflu.Shared.Resources.Fonts;
using System.ComponentModel;
using System.Reflection;
using System.Windows.Controls;
using System.Windows.Data;
using Usuel.Shared;

namespace Automation.App.Components.Inputs
{
    public class IconItem
    {
        public string Name { get; set; } = "";

        public string Icon { get; set; } = "";
    }

    /// <summary>
    /// Logique d'interaction pour SelectIconModal.xaml
    /// </summary>
    public partial class SelectIconModal : UserControl, IModalContent, INotifyPropertyChanged
    {
        public IModal? ParentLayout { get; set; }
        public ModalOptions Options { get; } = new ModalOptions() { Title = "Select icon" };

        public List<IconItem> Icons { get; private set; }
        public ICollectionView IconsView { get; private set; }

        public string SearchText { get; set; } = string.Empty;

        public ICustomCommand SelectCommand { get; }
        public IconItem? Selected { get; set; }

        public SelectIconModal()
        {
            SelectCommand = new DelegateCommand(() => ParentLayout?.Hide(true), () => Selected != null);
            Icons = LoadIcons();
            IconsView = CollectionViewSource.GetDefaultView(Icons);
            IconsView.Filter = (item) =>
            {
                if (string.IsNullOrEmpty(SearchText))
                    return true;

                IconItem? icon = item as IconItem;
                return icon != null && icon.Name.ToLower().Contains(SearchText.ToLower());
            };
            this.PropertyChanged += SelectIconModal_PropertyChanged;
            InitializeComponent();
        }

        private void SelectIconModal_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SearchText))
            {
                IconsView.Refresh();
            }
        }

        private List<IconItem> LoadIcons()
        {
            List<IconItem> icons = new List<IconItem>();

            Type type = typeof(IconFont);
            FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Static);

            foreach (FieldInfo field in fields)
            {
                string propertyName = field.Name;
                string? propertyValue = field.GetValue(null) as string; // null for static properties

                if (string.IsNullOrEmpty(propertyValue))
                    continue;
                icons.Add(new IconItem() { Name = propertyName, Icon = propertyValue });
            }
            return icons;
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectCommand.RaiseCanExecuteChanged();
        }
    }
}
