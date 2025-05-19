using Automation.Dal.Models;
using Automation.Plugins.Shared;
using Automation.Realtime.Clients;
using Automation.Shared.Packages;
using MongoDB.Bson;
using StackExchange.Redis;

namespace Automation.Worker
{
    /// <summary>
    /// Execute a task.
    /// </summary>
    public class RemoteTaskExecutor : ITaskExecutor
    {
        private readonly ConnectionMultiplexer _connection;
        // To send task progress to clients
        private readonly IPackageManagement _packages;
        private TasksRealtimeClient? _tasksClient;

        public RemoteTaskExecutor(ConnectionMultiplexer connection, IPackageManagement packageManagement)
        {
            _connection = connection;
            _packages = packageManagement;
        }

        public async Task<TaskInstance> ExecuteAsync(TaskInstance instance)
        {
            _tasksClient = new TasksRealtimeClient(_connection, instance.Id);
            _tasksClient.Lifecycle.Notify(instance.State);

            ITask task = await _packages.CreateTaskInstanceAsync(instance.Target);
            try
            {
                instance.State = await task.DoAsync(
                    new TaskContext() { SettingsJson = instance.Context.Settings.ToJson() },
                    new Progress<TaskProgress>(_tasksClient.Progress.Notify)
                );
                if (task is IResultsTask resultTask)
                    instance.Results = resultTask.Results;
            }
            catch
            {
                instance.State = EnumTaskState.Failed;
            }
            _tasksClient.Lifecycle.Notify(instance.State);
            return instance;
        }
    }
}
