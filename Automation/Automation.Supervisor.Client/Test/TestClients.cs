using Automation.Shared.Data;
using System.Text.Json;

namespace Automation.Supervisor.Client.Test
{
    /// <summary>
    /// Test repository for the supervisor
    /// </summary>

    public class TestTaskClient : ITaskClient
    {
        private readonly FakeApi _api = new FakeApi();

        public Task<TaskHistories> GetHistoryAsync(Guid taskId, int pageSize, int page)
        {
            return Task.FromResult(JsonSerializer.Deserialize<TaskHistories>(_api.GetTaskHistory(taskId, pageSize, page)));
        }

        public Task<TaskNode?> GetTaskAsync(Guid id)
        {
            return GetTaskAsync<TaskNode>(id);
        }

        public Task<T?> GetTaskAsync<T>(Guid id) where T : TaskNode
        {
            return Task.FromResult(JsonSerializer.Deserialize<T>(_api.GetTask(id)));
        }
    }

    public class TestScopeClient : IScopeClient
    {
        private readonly FakeApi _api = new FakeApi();

        public Task<TaskHistories> GetHistoryAsync(Guid scopeId, int pageSize, int page)
        {
            return Task.FromResult(JsonSerializer.Deserialize<TaskHistories>(_api.GetScopeHistory(scopeId, pageSize, page)));
        }

        public Task<Scope?> GetScopeAsync(Guid id)
        {
            return GetScopeAsync<Scope>(id);
        }

        public Task<T?> GetScopeAsync<T>(Guid id) where T : Scope
        {
            return Task.FromResult(JsonSerializer.Deserialize<T>(_api.GetScope(id)));
        }
    }
}
