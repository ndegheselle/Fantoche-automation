using Automation.Shared.Data.Scoped;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Automation.App.ViewModels.Scoped
{
    /// <summary>UI tab to focus when an element is opened (was on the drifted Automation.Models.Work).</summary>
    public enum EnumScopedTab
    {
        Default,
        History,
        Settings
    }

    /// <summary>
    /// MIGRATION: UI wrapper around an <see cref="ScopedElement"/> domain model. Carries the
    /// per-element UI state that previously lived on the drifted Automation.Models.Work partials
    /// (FocusOn, selection). Passive: data operations live in page view-models via the services;
    /// these wrappers only hold state and (for scopes) the child tree. Exposes <see cref="Metadata"/>
    /// directly so existing bindings (e.g. ScopedIcon Metadata="{Binding Metadata}") keep working.
    /// </summary>
    public abstract partial class ScopedElementViewModel : ObservableObject
    {
        public ScopedElement Model { get; }

        public ScopedMetadata Metadata => Model.Metadata;
        public EnumScopedType Type => Model.Metadata.Type;
        public Guid Id => Model.Id;

        [ObservableProperty]
        private EnumScopedTab _focusOn = EnumScopedTab.Default;

        [ObservableProperty]
        private bool _isSelected;

        protected ScopedElementViewModel(ScopedElement model) => Model = model;
    }
}
