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

        public Task<TaskHistories> GetHistoryAsync(Guid taskId, int page, int pageSize)
        {
            return Task.FromResult(JsonSerializer.Deserialize<TaskHistories>(_api.GetTaskHistory(taskId, pageSize, page)));
        }

        public Task<TaskNode?> GetTaskAsync(Guid id)
        {
            return Task.FromResult(JsonSerializer.Deserialize<TaskNode>(_api.GetTask(id)));
        }

        public Task<WorkflowNode?> GetWorkflowAsync(Guid id)
        {
            return Task.FromResult(JsonSerializer.Deserialize<WorkflowNode>(_api.GetWorkflow(id)));
        }

        public Task<TaskNode> CreateTaskAsync(TaskNode task)
        {
            throw new NotImplementedException();
        }

        public Task<WorkflowNode> CreateWorkflowAsync(WorkflowNode workflow)
        {
            throw new NotImplementedException();
        }

        public Task<TaskNode> UpdateTaskAsync(TaskNode task)
        {
            throw new NotImplementedException();
        }

        public Task<WorkflowNode> UpdateWorkflowAsync(WorkflowNode workflow)
        {
            throw new NotImplementedException();
        }
    }

    public class TestScopeClient : IScopeClient
    {
        private readonly FakeApi _api = new FakeApi();

        public Task<TaskHistories> GetHistoryAsync(Guid scopeId, int page, int pageSize)
        {
            return Task.FromResult(JsonSerializer.Deserialize<TaskHistories>(_api.GetScopeHistory(scopeId, pageSize, page)));
        }

        public Task<Scope> GetRootScopeAsync()
        {
            return Task.FromResult(JsonSerializer.Deserialize<Scope>(_api.GetScope(Guid.Parse("00000000-0000-0000-0000-000000000001"))));
        }

        public Task<Scope?> GetScopeAsync(Guid id)
        {
            return Task.FromResult(JsonSerializer.Deserialize<Scope>(_api.GetScope(id)));
        }

        public Task<Scope> CreateScopeAsync(Scope scope)
        {
            throw new NotImplementedException();
        }

        public Task<Scope> UpdateScopeAsync(Scope scope)
        {
            throw new NotImplementedException();
        }
    }
}
