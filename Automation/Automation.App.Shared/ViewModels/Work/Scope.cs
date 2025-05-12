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
    public class ScopedElement : ErrorValidationModel, IScopedElement, INotifyPropertyChanged
    {
        public string? Color { get; set; }
        public string? Icon { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;
        public void NotifyPropertyChanged([CallerMemberName] string? propertyName = null)
        { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)); }

        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public Guid? ParentId { get; set; }

        public EnumScopedType Type { get; set; }
        public EnumScopedTabs FocusOn { get; set; } = EnumScopedTabs.History;

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

        public Scope()
        {
            Type = EnumScopedType.Scope;
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
            SortedChildrens.SortDescriptions.Add(new SortDescription(nameof(Type), ListSortDirection.Ascending));
            SortedChildrens.SortDescriptions.Add(new SortDescription(nameof(Name), ListSortDirection.Ascending));
        }

        public void AddChild(ScopedElement element)
        {
            element.Parent = this;
            Childrens.Add(element);
        }
    }
}
