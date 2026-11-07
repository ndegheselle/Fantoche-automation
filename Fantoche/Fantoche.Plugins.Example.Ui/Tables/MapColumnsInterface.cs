using Fantoche.Plugins.Example.Tasks.Tables;
using Fantoche.Plugins.Shared;
using Newtonsoft.Json.Linq;

namespace Fantoche.Plugins.Example.Ui.Tables;

/// <summary>
/// The interface of <see cref="MapColumnsTask"/>.
/// <list type="bullet">
/// <item>Outside a run (<see cref="EnumInterfaceMode.Setup"/>), the user writes the names of the
/// workflow and the column each is expected to stand for : the table isn't read yet.</item>
/// <item>In a run, the user picks the column of the file each name stands for, and names the other
/// columns they need, previewed on the first rows.</item>
/// </list>
/// </summary>
[TaskInterface(typeof(MapColumnsTask))]
public class MapColumnsInterface : ITaskInterface
{
    /// <summary>What the view shown confirms, set once it is shown.</summary>
    private Func<JToken>? _answer;

    public bool Handles(EnumInterfaceMode mode) => mode is EnumInterfaceMode.Setup or EnumInterfaceMode.Execution;

    public object Show(TaskInterfaceContext context)
        => context.Mode == EnumInterfaceMode.Setup ? ShowSetup(context) : ShowRun(context);

    public Task<JToken?> ReadAsync()
    {
        Func<JToken> answer = _answer ?? throw new InvalidOperationException("The interface isn't shown.");
        return Task.FromResult<JToken?>(answer());
    }

    private MapColumnsSetupView ShowSetup(TaskInterfaceContext context)
    {
        // Started or scheduled on its own, the task is handed a table written whole rather than a
        // reference to one : the generated form edits it.
        if (context.References.Count == 0)
            throw new InvalidOperationException("The table is only pointed at from a workflow, where the node reads it.");

        JObject parameters = context.Parameters as JObject ?? new JObject();
        MapColumnsSetupViewModel names = new(parameters, context.References);
        _answer = names.Answer;
        return new MapColumnsSetupView { DataContext = names };
    }

    private MapColumnsView ShowRun(TaskInterfaceContext context)
    {
        MapColumnsParameters parameters = context.ParametersAs<MapColumnsParameters>() ?? new MapColumnsParameters();
        ITaskRuntime runtime = context.Runtime
            ?? throw new InvalidOperationException("The columns are picked in a run, where the table can be read.");

        MapColumnsViewModel columns = new(parameters, runtime.Storage);
        _ = columns.LoadAsync();
        _answer = () => AnswerOf(columns);
        return new MapColumnsView { DataContext = columns };
    }

    private static JToken AnswerOf(MapColumnsViewModel columns)
    {
        MapColumnsParameters answer = columns.Answer();

        // The rule of the task rather than a copy : what it refuses, the run would fail on.
        string? refusal = MapColumnsTask.Check(answer);
        if (refusal != null)
            throw new TaskInterfaceException(refusal);
        return JToken.FromObject(answer);
    }
}
