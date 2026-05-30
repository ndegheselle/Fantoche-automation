using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Avalonia;
using Usuel.Shared;

namespace Automation.Models.Work
{
    public enum EnumScopedTabs
    {
        Default,
        History,
        Settings
    }

    public partial class GraphNode
    {
        public Point Position { get; set; }
    }

    public abstract partial class ScopedElement : BaseErrorModel, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        public void RaisePropertyChanged([CallerMemberName] string? propertyName = null)
        { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)); }

        [JsonIgnore]
        public EnumScopedTabs FocusOn { get; set; } = EnumScopedTabs.History;
    }

    public partial class Scope : ScopedElement
    {
        // MIGRATION (WPF -> Avalonia): replaced WPF's ListCollectionView / CollectionViewSource
        // (System.Windows.Data, not available in Avalonia) with a plain sorted snapshot.
        // TODO (Phase 4 - Scopes views): this no longer re-sorts live on Childrens changes.
        // If live sorting is needed, reconcile with an Avalonia approach (e.g. sort in the
        // view, or DataGridCollectionView) when porting the Scopes UI.
        [JsonIgnore]
        public IReadOnlyList<ScopedElement>? SortedChildrens { get; set; }

        public void Refresh()
        {
            foreach (var child in Childrens)
            {
                child.Parent = this;
                if (child is Scope subScope)
                    subScope.Refresh();
            }
            SortedChildrens = Childrens
                .OrderBy(c => c.Metadata.Type)
                .ThenBy(c => c.Metadata.Name)
                .ToList();
            RaisePropertyChanged(nameof(SortedChildrens));
        }
    }
}
