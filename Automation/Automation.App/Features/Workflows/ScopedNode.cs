using System.Collections.ObjectModel;
using System.ComponentModel;
using Automation.Shared.Data.Scoped;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Automation.App.Features.Workflows
{
    /// <summary>
    /// A <see cref="ScopedElement"/> as displayed in the tree : it knows its parent, so a path can
    /// be built from it.
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

        /// <summary>Ancestors then itself, root first.</summary>
        public IEnumerable<ScopedNode> Path => Parent == null ? [this] : Parent.Path.Append(this);

        [ObservableProperty] private bool _isExpanded;
        [ObservableProperty] private bool _isSelected;

        public ScopedNode(ScopedElement element, ScopedNode? parent)
        {
            Element = element;
            Parent = parent;
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

        /// <summary>
        /// Fill the branch from an already loaded flat list of elements, indexed by their parent :
        /// the whole tree is read in one go, and so are the results of a search along with the
        /// scopes leading to them.
        /// </summary>
        public void Load(ILookup<Guid?, ScopedElement> byParent)
        {
            Children.Clear();
            foreach (ScopedElement child in byParent[Element.Id])
            {
                var node = new ScopedNode(child, this);
                Children.Add(node);
                node.Load(byParent);
            }
        }

        /// <summary>
        /// The node standing for [elementId] within this branch, null when it holds none : the tree
        /// can be displaying only part of itself while a search is on.
        /// </summary>
        public ScopedNode? Find(Guid elementId)
        {
            if (Element.Id == elementId)
                return this;

            return Children.Select(child => child.Find(elementId)).FirstOrDefault(found => found != null);
        }

        /// <summary>Open every branch under this node, what it holds being buried otherwise.</summary>
        public void ExpandAll()
        {
            IsExpanded = true;
            foreach (ScopedNode child in Children)
                child.ExpandAll();
        }
    }
}
