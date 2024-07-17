using Automation.Shared.ViewModels;
using System.Reflection;
using System.Text.Json;

namespace Automation.Supervisor.Client.Test
{
    /// <summary>
    /// Test repository for the supervisor
    /// </summary>
    public class TestClients : ITaskClient, IScopeClient
    {
        public TestClients()
        {
            // string test = CreateTestNodes();
        }

        public Scope GetRootScope() { return (Scope)GetScoped(Guid.Parse("00000000-0000-0000-0000-000000000001")); }

        public Task<Scope> GetRootScopeAsync() { return Task.FromResult(GetRootScope()); }

        public ScopedElement? GetScoped(Guid id)
        {
            var testData = TestDataFactory.LoadTestData();
            ScopedElement? scoped = testData.ScopedElements.FirstOrDefault(x => x.Id == id);

            // Load scope children
            if (scoped is Scope scope)
            {
                foreach (ScopedElement child in testData.ScopedElements
                    .Where(x => x.ParentId == scope.Id)
                    .OrderBy(x => x.Type)
                    .ThenBy(x => x.Name))
                {
                    scope.AddChild(child);
                }
            }

            return scoped;
        }

        public Task<ScopedElement?> GetScopedAsync(Guid id) { return Task.FromResult(GetScoped(id)); }

        public INode? GetNode(Guid id)
        {
            var testData = TestDataFactory.LoadTestData();
            INode? node = testData.Nodes.FirstOrDefault(x => x.Id == id);

            if (node == null)
                return null;

            // Make the links between the objects
            if (node is TaskNode task)
            {
                // For each inputs set the parent node
                foreach (NodeInput input in testData.Connectors
                    .Where(x => x is NodeInput && x.ParentId == task.Id)
                    .OrderByDescending(x => x.Type))
                {
                    task.AddInput(input);
                }
                // For each outputs set the parent node
                foreach (NodeOutput output in testData.Connectors
                    .Where(x => x is NodeOutput && x.ParentId == task.Id)
                    .OrderByDescending(x => x.Type))
                {
                    task.AddOutput(output);
                }
            }
            if (node is WorkflowNode workflow)
            {
                // Get the nodes
                foreach (Guid childId in testData.WorkflowRelations
                    .Where(x => x.WorkflowId == workflow.Id)
                    .Select(x => x.NodeId))
                {
                    INode childNode = GetNode(childId);
                    workflow.Nodes.Add(childNode);
                }

                // Get the connections
                foreach (NodeConnection connection in testData.Connections.Where(x => x.ParentId == workflow.Id))
                {
                    connection.Source = workflow.Nodes
                        .Where(x => x is INode)
                        .SelectMany(x => ((TaskNode)x).Outputs)
                        .First(x => x.Id == connection.SourceId);
                    connection.Target = workflow.Nodes
                        .Where(x => x is INode)
                        .SelectMany(x => ((TaskNode)x).Inputs)
                        .First(x => x.Id == connection.TargetId);
                    workflow.AddConnection(new NodeConnection(workflow, connection.Source, connection.Target));
                }
            }

            return node;
        }

        public Task<INode?> GetNodeAsync(Guid id) { return Task.FromResult(GetNode(id)); }

        public IEnumerable<TaskInstance> GetScopedInstances(Guid taskId, int number, int page)
        {
            var testData = TestDataFactory.LoadTestData();
            List<TaskInstance> testList = new List<TaskInstance>();
            for (int i = page * number; i < page * number + number; i++)
            {
                var testInstance = new TaskInstance()
                {
                    Id = Guid.NewGuid(),
                    TaskId = taskId,
                    Task = (INode)testData.Nodes.First(x => x.Id == taskId),
                    ParentTask = testData.Nodes.FirstOrDefault(x => x is WorkflowNode) as INode,
                    StartDate = DateTime.Now,
                    EndDate = DateTime.Now.AddSeconds(10)
                };
                testList.Add(testInstance);
                testInstance.Status = (EnumInstanceStatus)(i % 5);
            }
            return testList;
        }

        public Task<IEnumerable<TaskInstance>> GetScopedInstancesAsync(Guid taskId, int number, int page)
        { return Task.FromResult(GetScopedInstances(taskId, number, page)); }

        public int GetScopedInstancesCount(Guid taskId) { return 100000; }

        public Task<int> GetScopedInstancesCountAsync(Guid taskId)
        { return Task.FromResult(GetScopedInstancesCount(taskId)); }
    }
}
