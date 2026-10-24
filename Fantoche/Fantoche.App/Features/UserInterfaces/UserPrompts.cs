using Fantoche.Shared.Data.Execution;
using Fantoche.Shared.Data.Scoped;
using Fantoche.Shared.Services;
using Fantoche.Worker;
using Fantoche.Worker.Storage;

namespace Fantoche.App.Features.UserInterfaces
{
    /// <summary>
    /// The user interface tasks waiting for the user : their page shown in a window of its own, the
    /// answer stored, and the run they hold resumed. Any copy of the application able to resume a
    /// run answers it (see <see cref="UserAnswers"/>).
    /// </summary>
    public sealed class UserPrompts
    {
        private readonly IScopedService _scoped;
        private readonly IHistoryService _history;
        private readonly IExecutionService _execution;
        private readonly IDispatchService _dispatch;
        private readonly LocalWorker _worker;
        private readonly string _applicationId;
        private readonly string _browserFolder;

        /// <param name="worker">What the pages are extracted with, from the package of their task.</param>
        /// <param name="browserFolder">Where the browser showing the pages keeps its data.</param>
        public UserPrompts(
            IScopedService scoped,
            IHistoryService history,
            IExecutionService execution,
            IDispatchService dispatch,
            LocalWorker worker,
            string applicationId,
            string browserFolder)
        {
            _scoped = scoped;
            _history = history;
            _execution = execution;
            _dispatch = dispatch;
            _worker = worker;
            _applicationId = applicationId;
            _browserFolder = browserFolder;
        }

        /// <summary>
        /// The tasks [run] waits on the user for : the run itself for a task run alone, the nested
        /// runs holding them left out.
        /// </summary>
        public async Task<IReadOnlyList<TaskInstance>> GetAwaitingAsync(TaskInstance run)
        {
            if (run.State != EnumTaskState.AwaitingUser)
                return [];

            IReadOnlyList<TaskInstance> descendants = await _history.GetDescendantsAsync(run.Id);
            if (descendants.Count == 0)
                return [run];

            List<TaskInstance> awaiting = [.. descendants.Where(x => x.State == EnumTaskState.AwaitingUser)];
            return [.. awaiting.Where(x => !awaiting.Any(nested => nested.ParentInstanceId == x.Id))];
        }

        /// <summary>
        /// Show the page of [instance], a task waiting for the user, and once it is answered resume
        /// the run it holds. Returns that run, or the task itself when it ran alone : null when the
        /// page was closed without an answer.
        /// </summary>
        /// <exception cref="ExecutionException">[instance] can't be answered, or its run resumed.</exception>
        public async Task<TaskInstance?> AnswerAsync(TaskInstance instance)
        {
            // Checked before the page is shown : what the user fills in would be refused afterwards.
            TaskInstance answerable = await UserAnswers.GetAnswerableRunAsync(_history, instance, _applicationId);
            if (await _scoped.LoadRunnableAsync(instance.TaskId) is not AutomationTask task)
                throw new ExecutionException($"'{instance.NodeName}' isn't a task.");
            string folder = await _worker.GetPageFolderAsync(task);
            LocalStorage storage = _worker.OpenStorage(answerable);

            TaskInstance? run = null;
            bool isAnswered = await PageWindow.ShowAsync(instance.NodeName, folder, storage, instance.Parameters, _browserFolder, async answer =>
            {
                try
                {
                    run = await UserAnswers.SubmitAsync(_scoped, _history, instance.Id, answer, _applicationId);
                    return null;
                }
                catch (ExecutionException exception)
                {
                    return exception.Message;
                }
            });

            if (!isAnswered || run == null)
                return null;
            // A task run alone is over once answered.
            if (run.Id == instance.Id)
                return run;
            return await ResumeAsync(run);
        }

        /// <summary>Resume [run] where it is run : by this application, or on the workers of the supervisor.</summary>
        private async Task<TaskInstance> ResumeAsync(TaskInstance run)
        {
            if (run.ApplicationId != null)
                return await _execution.ResumeAsync(run.Id);

            await _dispatch.ResumeAsync(run.Id);
            return await _history.GetAsync(run.Id) ?? run;
        }
    }
}
