using CommunityToolkit.Mvvm.Input;

namespace Automation.App.Features.Scoped.Workflows.Editor.ViewModels;

/// <summary>
/// Drives the Nodify pending-connection interaction: remembers the connector a drag started from
/// and creates (or toggles off) a connection when it completes on another connector.
/// </summary>
internal class PendingConnectionVm
{
    private readonly EditorVm _editor;
    private ConnectorVm? _source;

    public IRelayCommand<ConnectorVm> StartCommand { get; }
    public IRelayCommand<ConnectorVm> FinishCommand { get; }

    public PendingConnectionVm(EditorVm editor)
    {
        _editor = editor;
        StartCommand = new RelayCommand<ConnectorVm>(source => _source = source);
        FinishCommand = new RelayCommand<ConnectorVm>(target =>
        {
            ConnectorVm? source = _source;
            _source = null;

            if (target == null || source == null || source == target)
                return;

            _editor.ToggleConnection(source, target);
        });
    }
}
