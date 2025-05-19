using Automation.Dal.Models;
using Automation.Dal.Repositories;
using Automation.Plugins.Shared;
using Automation.Realtime;
using Automation.Realtime.Clients;
using Automation.Shared.Data;
using Automation.Shared.Packages;
using MongoDB.Bson;
using MongoDB.Driver;
using NuGet.Configuration;
using NuGet.Protocol.Core.Types;
using StackExchange.Redis;

namespace Automation.Worker
{
    /// <summary>
    /// Execute a task.
    /// </summary>
    public class LocalTaskExecutor : ITaskExecutor
    {
        private readonly RedisConnectionManager _redis;
        private readonly TaskIntancesRepository _instanceRepo;
        private readonly TasksRepository _taskRepo;

        // To send task progress to clients
        private readonly IPackageManagement _packages;
        private TasksRealtimeClient? _tasksClient;

        public LocalTaskExecutor(IMongoDatabase database, RedisConnectionManager connection, IPackageManagement packageManagement)
        {
            _instanceRepo = new TaskIntancesRepository(database);
            _taskRepo = new TasksRepository(database);
            _redis = connection;
            _packages = packageManagement;
        }

        public async Task<EnumTaskState> ExecuteAsync(TargetedPackage package, TaskContext context, IProgress<TaskProgress> progress)
        {
            ITask task = await _packages.CreateTaskInstanceAsync(package);
            try
            {
                return await task.DoAsync(
                    context,
                    progress
                );
            }
            catch
            {
            }

            return EnumTaskState.Failed;
        }
    }
}
