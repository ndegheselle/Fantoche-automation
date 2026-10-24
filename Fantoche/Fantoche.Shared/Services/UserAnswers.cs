using Fantoche.Shared.Data.Execution;
using Fantoche.Shared.Data.Scoped;
using Newtonsoft.Json.Linq;

namespace Fantoche.Shared.Services;

/// <summary>
/// The answers of the users to the user interface tasks : checked against what the task hands
/// over, then stored as its output. The run it belongs to is resumed afterwards, by whoever resumes
/// it (see <see cref="RunResumption"/>).
/// </summary>
public static class UserAnswers
{
    /// <summary>
    /// Answer [instanceId], a task waiting for the user, with [answer] on behalf of [applicationId].
    /// Returns the run it belongs to, to be resumed : the instance itself, completed, when it ran
    /// on its own.
    /// </summary>
    /// <exception cref="ExecutionException">
    /// The task doesn't wait for an answer anymore, the answer doesn't fit its output, or
    /// [applicationId] can't resume the run it belongs to.
    /// </exception>
    public static async Task<TaskInstance> SubmitAsync(
        IScopedService scoped,
        IHistoryService history,
        Guid instanceId,
        JToken answer,
        string applicationId)
    {
        TaskInstance instance = await history.GetAsync(instanceId)
            ?? throw new ExecutionException($"Unknown execution '{instanceId}'.");
        TaskInstance run = await GetAnswerableRunAsync(history, instance, applicationId);

        BaseAutomationTask task = await scoped.LoadRunnableAsync(instance.TaskId);
        var errors = task.OutputSchema?.Validate(answer) ?? [];
        if (errors.Count > 0)
            throw new ExecutionException($"The answer doesn't fit what '{instance.NodeName}' hands over : {string.Join(", ", errors)}");

        if (!await history.AnswerAsync(instanceId, answer))
            throw new ExecutionException($"'{instance.NodeName}' was answered meanwhile.");

        return run.Id == instance.Id
            ? await history.GetAsync(run.Id) ?? run
            : run;
    }

    /// <summary>
    /// The run [instance] belongs to, when [applicationId] can answer it : the task waits for the
    /// user, and nothing runs its run anymore. Checked before the page is shown as well, so a user
    /// doesn't fill in a page whose answer would be refused.
    /// </summary>
    /// <exception cref="ExecutionException">The reason [instance] can't be answered.</exception>
    public static async Task<TaskInstance> GetAnswerableRunAsync(IHistoryService history, TaskInstance instance, string applicationId)
    {
        if (instance.State != EnumTaskState.AwaitingUser)
            throw new ExecutionException($"'{instance.NodeName}' doesn't wait for an answer, it is {instance.State}.");

        TaskInstance run = await RunOfAsync(history, instance);
        // Answered by an application that can't resume the run, it would be held for good.
        if (run.ApplicationId != null && run.ApplicationId != applicationId)
            throw new ExecutionException($"The run was started by the application '{run.ApplicationId}' : answer it from there.");

        // Its other branches still running, the run would end held by nothing left to resume it.
        if (run.State != EnumTaskState.AwaitingUser)
            throw new ExecutionException("The run still runs other nodes : answer once it waits for you.");

        return run;
    }

    /// <summary>The run [instance] belongs to : the instance of the workflow at the top.</summary>
    private static async Task<TaskInstance> RunOfAsync(IHistoryService history, TaskInstance instance)
    {
        TaskInstance run = instance;
        while (run.ParentInstanceId is Guid parentId)
        {
            run = await history.GetAsync(parentId)
                ?? throw new ExecutionException($"The run of '{instance.NodeName}' isn't in the history anymore.");
        }
        return run;
    }
}
