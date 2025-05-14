using Automation.Shared.Data;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Windows.Data;
using Usuel.Shared;

namespace Automation.App.Shared.ViewModels.Work
{
    public enum EnumScopedTabs
    {
        Default,
        History,
        Settings
    }

    [JsonDerivedType(typeof(AutomationTask), "task")]
    [JsonDerivedType(typeof(AutomationWorkflow), "workflow")]
    [JsonDerivedType(typeof(Scope), "scope")]
    public abstract class ScopedElement : ErrorValidationModel, IScopedElement, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        public void NotifyPropertyChanged([CallerMemberName] string? propertyName = null)
        { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)); }

        public ScopedMetadata Metadata { get; set; }

        public Guid Id { get; set; }
        public Guid? ParentId { get; set; }

        public EnumScopedTabs FocusOn { get; set; } = EnumScopedTabs.History;

        public ScopedElement(EnumScopedType type)
        {
            Metadata = new ScopedMetadata(type);
        }

        #region UI specific
        [JsonIgnore]
        public Scope? Parent { get; set; }

        public void ChangeParent(Scope scope)
        {
            Parent = scope;
            ParentId = scope.Id;
        }
        #endregion
    }

    public class Scope : ScopedElement
    {
        public Dictionary<string, string> Context { get; set; } = new Dictionary<string, string>();
        public ObservableCollection<ScopedElement> Childrens { get; set; }

        [JsonIgnore]
        public ListCollectionView? SortedChildrens { get; set; }

        public Scope() : base(EnumScopedType.Scope)
        {
            Childrens = [];
        }

        public void RefreshChildrens()
        {
            foreach (var child in Childrens)
            {
                child.Parent = this;
                if (child is Scope subScope)
                    subScope.RefreshChildrens();
            }
            SortedChildrens = (ListCollectionView)CollectionViewSource.GetDefaultView(Childrens);
            SortedChildrens.SortDescriptions.Add(new SortDescription(nameof(Metadata.Type), ListSortDirection.Ascending));
            SortedChildrens.SortDescriptions.Add(new SortDescription(nameof(Metadata.Name), ListSortDirection.Ascending));
        }

        public void AddChild(ScopedElement element)
        {
            element.Parent = this;
            Childrens.Add(element);
        }
    }
}
