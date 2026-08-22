using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Automation.Shared.Data.Scoped
{
    [Flags]
    public enum EnumScopedType
    {
        Scope,
        Workflow,
        Task
    }

    /// <summary>
    /// What every scoped element is presented with : its name and how it is displayed. It notifies its
    /// changes so the views editing it can bind to it directly.
    /// </summary>
    public partial class ScopedMetadata : ObservableObject
    {
        [ObservableProperty] private EnumScopedType _type;
        [ObservableProperty] private string _name = "";
        [ObservableProperty] private string? _color;
        [ObservableProperty] private string? _icon;
        [ObservableProperty] private bool _isReadOnly;
        [ObservableProperty] private ObservableCollection<string> _tags = [];

        public ScopedMetadata()
        {
        }

        public ScopedMetadata(EnumScopedType type)
        {
            Type = type;
        }

        public ScopedMetadata(string name, EnumScopedType type)
        {
            Name = name;
            Type = type;
        }

        /// <summary>
        /// Copy of the metadata, holding its own tags and no subscriber of the original.
        /// </summary>
        public ScopedMetadata Clone()
        {
            return new ScopedMetadata(Name, Type)
            {
                Color = Color,
                Icon = Icon,
                IsReadOnly = IsReadOnly,
                Tags = [.. Tags]
            };
        }
    }
}
