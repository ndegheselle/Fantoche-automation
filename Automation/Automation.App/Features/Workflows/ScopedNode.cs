using System.Collections.ObjectModel;
using System.ComponentModel;
using Automation.Shared.Data.Scoped;
using Automation.Shared.Services;
using CommunityToolkit.Mvvm.ComponentModel;

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

        public ScopedMetadata Metadata => Element.Metadata;
        public string Name => Element.Metadata.Name;
        public EnumScopedType Type => Element.Metadata.Type;
        public bool IsScope => Type == EnumScopedType.Scope;

        /// <summary>
        /// The element as something runnable, <see langword="null"/> for a scope : it is what a node
        /// dragged out of the tree carries.
        /// </summary>
        public BaseAutomationTask? TaskElement => Element as BaseAutomationTask;

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
            // Name and Type are read from the metadata, which the details pages edit directly.
            Element.Metadata.PropertyChanged += OnMetadataChanged;
        }

        private void OnMetadataChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ScopedMetadata.Name))
                OnPropertyChanged(nameof(Name));
            else if (e.PropertyName == nameof(ScopedMetadata.Type))
                OnPropertyChanged(nameof(Type));
        }

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
