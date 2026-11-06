using System.Windows;
using Fantoche.Plugins.Example.Tasks.Tables;
using Fantoche.Plugins.Shared;
using Fantoche.Plugins.Shared.Ui;
using Newtonsoft.Json.Linq;

namespace Fantoche.Plugins.Example.Ui.Tables;

/// <summary>
/// The interface of <see cref="MapColumnsTask"/> in a run : the user picks the column of the file each
/// name of the workflow stands for, and names the other columns they need, previewed on the first
/// rows. In the editor the table isn't read yet : the generated form edits the names there.
/// </summary>
public class MapColumnsInterface : ITaskInterface
{
    private MapColumnsViewModel? _columns;

    public bool Handles(EnumInterfaceMode mode) => mode == EnumInterfaceMode.Execution;

    public FrameworkElement Show(TaskInterfaceContext context)
    {
        MapColumnsParameters parameters = context.ParametersAs<MapColumnsParameters>() ?? new MapColumnsParameters();
        ITaskRuntime runtime = context.Runtime
            ?? throw new InvalidOperationException("The columns are picked in a run, where the table can be read.");

        _columns = new MapColumnsViewModel(parameters, runtime.Storage);
        _ = _columns.LoadAsync();
        return new MapColumnsView { DataContext = _columns };
    }

    public Task<JToken?> ReadAsync()
    {
        MapColumnsViewModel columns = _columns ?? throw new InvalidOperationException("The interface isn't shown.");
        MapColumnsParameters answer = columns.Answer();

        // The rule of the task rather than a copy : what it refuses, the run would fail on.
        string? refusal = MapColumnsTask.Check(answer);
        if (refusal != null)
            throw new TaskInterfaceException(refusal);
        return Task.FromResult<JToken?>(JToken.FromObject(answer));
    }
}
