using Automation.Dal;
using Automation.Dal.Repositories;
using Automation.Models.Work;
using Automation.Shared.Data;
using Automation.Shared.Data.Task;
using Newtonsoft.Json.Linq;

namespace Automation.Worker.Executor;

public class LocalWorkflowExecutor
{
    private readonly ScopesRepository _scopesRepo;

    private readonly LocalTaskExecutor _executor;
    private readonly AutomationWorkflow _workflow;

    public LocalWorkflowExecutor(DatabaseConnection connection, LocalTaskExecutor executor, AutomationWorkflow workflow)
    {
        _workflow = workflow;
        _scopesRepo = new ScopesRepository(connection);
        _executor = executor;
    }

    public async Task<TaskInstance> ExecuteAsync(
        TaskInstance instance,
        IProgress<TaskInstanceNotification>? progress = null)
    {
        await Start(instance);
        return instance;
    }

    private async Task Start(TaskInstance instance)
    {
        if (instance.Data == null)
            instance.Data = new TaskInstanceData();

        // Add global context
        instance.Data.GlobalToken = await _scopesRepo.GetContextFromTree(_workflow.ParentTree);

        foreach (var start in _workflow.Graph.GetStartNodes()) Next(start, instance);
    }

    private void End()
    {
    }

    private void Next(BaseGraphTask task, TaskInstance instance)
    {
        var nextTasks = _workflow.Graph.GetNextFrom(task);
        foreach (var next in nextTasks)
        {
            if (next.Settings.IsWaitingAllInputs)
                next.WaitedInputs.Add(task.Name, instance.Data.InputToken);

            if (_workflow.Graph.CanExecute(next))
                _ = Execute(next, instance);
        }
    }

    private async Task Execute(BaseGraphTask task, TaskInstance instance)
    {
        var subInstance = new SubTaskInstance(instance.Id, task);

        var context = _workflow.Graph.Execution.GetContextFor(task, instance.Data);
        if (task.Settings.IsWaitingAllInputs)
            task.WaitedInputs.Clear();

        JToken? input = null;
        if (string.IsNullOrEmpty(task.InputJson) == false)
            input = ReferencesHandler.ReplaceReferences(JToken.Parse(task.InputJson), context).ReplacedSetting;

        await _executor.ExecuteAsync(subInstance, null, );

        Next(task, instance);
    }
}