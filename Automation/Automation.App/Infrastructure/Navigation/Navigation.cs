// MIGRATION SHIM: ported 1:1 from the (now dropped) Joufflu.Shared library.
// Pure interfaces with no UI-framework dependency, so they port unchanged. The original
// Joufflu.Shared.Navigation namespace is preserved so migrated view code-behind keeps its
// existing `using Joufflu.Shared.Navigation;` with no change.
namespace Joufflu.Shared.Navigation
{
    /// <summary>
    /// Page for navigation systems
    /// </summary>
    public interface IPage
    {
        public void OnHide()
        { }
    }

    /// <summary>
    /// Page with a layout as a parent, allow the page to set data in the layout
    /// </summary>
    public interface IPage<TLayout> : IPage where TLayout : ILayout
    {
        public TLayout? ParentLayout { get; set; }
    }

    /// <summary>
    /// Page that can contain and display another page
    /// </summary>
    public interface ILayout : IPage
    {
        void Hide();
        void Show(IPage page);
    }

    /// <summary>
    /// Nested layout
    /// </summary>
    public interface ILayout<TLayout> : ILayout, IPage<TLayout> where TLayout : ILayout
    { }
}
