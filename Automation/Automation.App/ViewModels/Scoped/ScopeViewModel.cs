using System.Collections.ObjectModel;
using Automation.Shared.Data.Scoped;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Automation.App.ViewModels.Scoped
{
    /// <summary>
    /// UI wrapper for a <see cref="Scope"/>. Owns the sorted child tree (replacing the drifted
    /// Scope.SortedChildrens / Refresh()) and expansion state.
    /// </summary>
    public partial class ScopeViewModel : ScopedElementViewModel
    {
        public new Scope Model => (Scope)base.Model;

        /// <summary>Child element view-models, sorted by type then name.</summary>
        public ObservableCollection<ScopedElementViewModel> Children { get; } = [];

        [ObservableProperty]
        private bool _isExpanded;

        public ScopeViewModel(Scope model) : base(model) => RefreshChildren();

        /// <summary>Rebuilds the child view-models from the domain model (sorted by type then name).</summary>
        public void RefreshChildren()
        {
            Children.Clear();
            foreach (ScopedElement child in Model.Childrens
                .OrderBy(c => c.Metadata.Type)
                .ThenBy(c => c.Metadata.Name, StringComparer.OrdinalIgnoreCase))
            {
                Children.Add(ScopedElementViewModelFactory.Create(child));
            }
        }

        /// <summary>Adds a child to both the domain model and the (sorted) VM tree.</summary>
        public ScopedElementViewModel AddChild(ScopedElement element)
        {
            Model.AddChild(element);
            ScopedElementViewModel vm = ScopedElementViewModelFactory.Create(element);
            InsertSorted(vm);
            return vm;
        }

        public void RemoveChild(ScopedElementViewModel child)
        {
            Model.Childrens.Remove(child.Model);
            Children.Remove(child);
        }

        private void InsertSorted(ScopedElementViewModel vm)
        {
            int index = 0;
            while (index < Children.Count && Compare(Children[index], vm) <= 0)
                index++;
            Children.Insert(index, vm);
        }

        private static int Compare(ScopedElementViewModel a, ScopedElementViewModel b)
        {
            int byType = a.Type.CompareTo(b.Type);
            return byType != 0
                ? byType
                : string.Compare(a.Metadata.Name, b.Metadata.Name, StringComparison.OrdinalIgnoreCase);
        }
    }
}
