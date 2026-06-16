using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Automation.App.Features.Scoped.Workflows.Editor;

public partial class EditorGraph : UserControl
{
    private EditorVm? _editor;

    public EditorGraph()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, System.EventArgs e)
    {
        if (_editor != null)
        {
            _editor.ZoomInRequested -= OnZoomIn;
            _editor.ZoomOutRequested -= OnZoomOut;
            _editor.FitToScreenRequested -= OnFitToScreen;
        }

        _editor = DataContext as EditorVm;

        if (_editor != null)
        {
            _editor.ZoomInRequested += OnZoomIn;
            _editor.ZoomOutRequested += OnZoomOut;
            _editor.FitToScreenRequested += OnFitToScreen;
        }
    }

    private void OnZoomIn() => Editor.ZoomIn();
    private void OnZoomOut() => Editor.ZoomOut();
    private void OnFitToScreen() => Editor.FitToScreen(null);
}
