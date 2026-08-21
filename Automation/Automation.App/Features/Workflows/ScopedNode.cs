using Automation.Shared.Data.Scoped;
using Automation.Shared.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace Automation.App.Features.Workflows
{
    /// <summary>
    /// A <see cref="ScopedElement"/> as displayed in the tree : it knows its parent, so a path can be built from it.
    /// </summary>
    public partial class ScopedNode : ObservableObject
    {
        public ScopedElement Element { get; }
        public ScopedNode? Parent { get; }
        public ObservableCollection<ScopedNode> Children { get; } = [];

        public string Name => Element.Metadata.Name;
        public EnumScopedType Type => Element.Metadata.Type;
        public bool IsScope => Type == EnumScopedType.Scope;

        /// <summary>
        /// Ancestors then itself, root first.
        /// </summary>
        public IEnumerable<ScopedNode> Path => Parent == null ? [this] : Parent.Path.Append(this);

        [ObservableProperty] private bool _isExpanded;
        [ObservableProperty] private bool _isSelected;

        private readonly IScopedService _scoped;

        public ScopedNode(ScopedElement element, ScopedNode? parent, IScopedService scoped)
        {
            Element = element;
            Parent = parent;
            _scoped = scoped;
        }

        /// <summary>
        /// Notify the name changed, the metadata being edited through the element itself.
        /// </summary>
        public void NotifyNameChanged() => OnPropertyChanged(nameof(Name));

        public async Task LoadAsync()
        {
            if (!IsScope)
                return;

            Children.Clear();
            foreach (ScopedElement child in await _scoped.GetChildrensAsync(Element.Id))
            {
                var node = new ScopedNode(child, this, _scoped);
                Children.Add(node);
                await node.LoadAsync();
            }
        }
    }
}
