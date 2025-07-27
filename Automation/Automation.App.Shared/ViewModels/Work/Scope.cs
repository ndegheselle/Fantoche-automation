using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Windows.Data;
using Usuel.Shared;

namespace Automation.Dal.Models
{
    public enum EnumScopedTabs
    {
        Default,
        History,
        Settings
    }

    public abstract partial class ScopedElement : ErrorValidationModel, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        public void RaisePropertyChanged([CallerMemberName] string? propertyName = null)
        { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)); }

        [JsonIgnore]
        public EnumScopedTabs FocusOn { get; set; } = EnumScopedTabs.History;
    }

    public partial class Scope : ScopedElement
    {
        [JsonIgnore]
        public ListCollectionView? SortedChildrens { get; set; }

        public void Refresh()
        {
            foreach (var child in Childrens)
            {
                child.Parent = this;
                if (child is Scope subScope)
                    subScope.Refresh();
            }
            SortedChildrens = (ListCollectionView)CollectionViewSource.GetDefaultView(Childrens);
            SortedChildrens.SortDescriptions.Add(new SortDescription($"{nameof(Metadata)}.{nameof(Metadata.Type)}", ListSortDirection.Ascending));
            SortedChildrens.SortDescriptions.Add(new SortDescription($"{nameof(Metadata)}.{nameof(Metadata.Name)}", ListSortDirection.Ascending));
        }
    }
}
