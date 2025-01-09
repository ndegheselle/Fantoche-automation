using Automation.Dal.Models;
using Automation.Plugins.Shared;
using Automation.Realtime.Clients;
using Automation.Server.Shared.Packages;
using Automation.Shared.Data;
using MongoDB.Bson;
using StackExchange.Redis;

namespace Automation.Worker.Service.Business
{
    public class TaskExecutor : IProgress
    {
        private readonly ConnectionMultiplexer _connection;
        // To send task progress to clients
        private readonly IPackageManagement _packages;
        private TaskInstance? _currentInstance = null;
        private TasksRealtimeClient? _tasksClient;

        public TaskExecutor(ConnectionMultiplexer connection, IPackageManagement packageManagement)
        {
            _connection = connection;
            _packages = packageManagement;
        }

        public async Task<TaskInstance> ExecuteAsync(TaskInstance instance)
        {
            _currentInstance = instance;
            _tasksClient = new TasksRealtimeClient(_connection, instance.Id);
            _tasksClient.Lifecycle.Notify(_currentInstance.State);

            ITask task = await _packages.CreateTaskInstanceAsync(_currentInstance.Target);
            try
            {
                task.Progress = this;
                await task.ExecuteAsync(new TaskContext() { SettingsJson = _currentInstance.Context.Settings.ToJson() });
                if (task is IResultsTask resultTask)
                    instance.Results = resultTask.Results;
                instance.State = EnumTaskState.Completed;
            } catch
            {
                instance.State = EnumTaskState.Failed;
            }
            _tasksClient.Lifecycle.Notify(instance.State);
            return instance;
        }

        public void Send(TaskProgress progress)
        {
            if (_currentInstance == null)
                return;
            _tasksClient?.Progress.Notify(progress);
        }
    }
}
