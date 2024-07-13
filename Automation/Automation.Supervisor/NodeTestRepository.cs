using Automation.Shared.Supervisor;
using Automation.Shared.ViewModels;
using System.IO;
using System.Reflection;
using System.Text.Json;

namespace Automation.Supervisor.Repositories
{
    // Separated like it would be in a relationnal database
    public class TestData
    {
        public List<Node> Nodes { get; set; }

        public List<ScopedElement> ScopedElements { get; set; }

        public List<NodeConnector> Connectors { get; set; }

        public List<NodeConnection> Connections { get; set; }

        public List<WorkflowRelation> WorkflowRelations { get; set; }
    }

    /// <summary>
    /// Test repository for the supervisor
    /// </summary>
    public class NodeTestRepository : INodeRepository, IScopeRepository
    {
        public NodeTestRepository()
        {
            // CreateTestNodes();
        }

        public Scope GetRootScope() { return (Scope)GetScoped(Guid.Parse("00000000-0000-0000-0000-000000000001")); }

        public Task<Scope> GetRootScopeAsync()
        {
            return Task.FromResult(GetRootScope());
        }

        public ScopedElement? GetScoped(Guid id)
        {
            var testData = LoadTestData();
            ScopedElement? scoped = testData.ScopedElements.FirstOrDefault(x => x.Id == id);

            // Load scope children
            if (scoped is Scope scope)
            {
                foreach (ScopedElement child in testData.ScopedElements.Where(x => x.ParentId == scope.Id))
                {
                    scope.AddChild(child);
                }
            }

            return scoped;
        }

        public Task<ScopedElement?> GetScopedAsync(Guid id)
        {
            return Task.FromResult(GetScoped(id));
        }

        public Node? GetNode(Guid id)
        {
            var testData = LoadTestData();
            Node? node = testData.Nodes.FirstOrDefault(x => x.Id == id);

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
                    TaskNode childNode = (TaskNode)GetNode(childId);
                    workflow.Nodes.Add(childNode);
                }

                // Get the connections
                foreach (NodeConnection connection in testData.Connections.Where(x => x.ParentId == workflow.Id))
                {
                    connection.Source = workflow.Nodes
                        .Where(x => x is TaskNode)
                        .SelectMany(x => ((TaskNode)x).Outputs)
                        .First(x => x.Id == connection.SourceId);
                    connection.Target = workflow.Nodes
                        .Where(x => x is TaskNode)
                        .SelectMany(x => ((TaskNode)x).Inputs)
                        .First(x => x.Id == connection.TargetId);
                    workflow.AddConnection(new NodeConnection(workflow, connection.Source, connection.Target));
                }
            }

            return node;
        }

        public Task<Node?> GetNodeAsync(Guid id)
        {
            return Task.FromResult(GetNode(id));
        }

        public IEnumerable<TaskInstance> GetTaskInstances(Guid taskId, int number, int page)
        {
            List<TaskInstance> testList = new List<TaskInstance>();
            for (int i = page * number; i < page * number + number; i++)
            {
                var testInstance = new TaskInstance()
                {
                    Id = Guid.NewGuid(),
                    TaskId = taskId,
                    StartDate = DateTime.Now,
                    EndDate = DateTime.Now.AddSeconds(10)
                };
                testList.Add(testInstance);
                testInstance.Status = (EnumInstanceStatus)(i % 5);
            }
            return testList;
        }

        public Task<IEnumerable<TaskInstance>> GetTaskInstancesAsync(Guid taskId, int number, int page)
        {
            return Task.FromResult(GetTaskInstances(taskId, number, page));
        }

        public int GetTaskInstancesCount(Guid taskId)
        {
            return 100000;
        }

        public Task<int> GetTaskInstancesCountAsync(Guid taskId)
        {
            return Task.FromResult(GetTaskInstancesCount(taskId));
        }

        #region Debug
        private TestData LoadTestData()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "Automation.Supervisor.Resources.test.json";

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                using (StreamReader reader = new StreamReader(stream))
                {
                    string jsonFile = reader.ReadToEnd(); //Make string equal to full file
                    return JsonSerializer.Deserialize<TestData>(jsonFile);
                }
        }

        private void CreateTestNodes()
        {
            TaskNode taskScope1 = new TaskNode() { Name = "Task 1" };
            var flowIn1 = new NodeInput() { ParentId = taskScope1.Id, Type = EnumNodeConnectorType.Flow };
            var flowOut1 = new NodeOutput() { ParentId = taskScope1.Id, Type = EnumNodeConnectorType.Flow };
            var input1 = new NodeInput() { Name = "Input 2", ParentId = taskScope1.Id };
            var output1 = new NodeOutput() { Name = "Output 2", ParentId = taskScope1.Id };

            TaskNode taskScope2 = new TaskNode() { Name = "Task 2", };
            var flowIn2 = new NodeInput() { ParentId = taskScope2.Id, Type = EnumNodeConnectorType.Flow };
            var flowOut2 = new NodeOutput() { ParentId = taskScope2.Id, Type = EnumNodeConnectorType.Flow };
            var input2 = new NodeInput() { Name = "Input 1", ParentId = taskScope2.Id };
            var output2 = new NodeOutput() { Name = "Output 1", ParentId = taskScope2.Id };

            WorkflowInputNode workflowInput = new WorkflowInputNode() { Name = "Start", };
            var flowOut3 = new NodeOutput() { ParentId = workflowInput.Id, Type = EnumNodeConnectorType.Flow };

            WorkflowInputNode workflowOutput = new WorkflowInputNode() { Name = "End", };
            var flowIn3 = new NodeInput() { ParentId = workflowOutput.Id, Type = EnumNodeConnectorType.Flow };

            WorkflowNode workflowScope = new WorkflowNode() { Name = "Workflow 1", };

            var connection = new NodeConnection(workflowScope, output2, input1);
            #region Scoped elements
            Scope subScope = new Scope() { Name = "SubScope 1", };
            subScope.Context = new Dictionary<string, string>()
            {
                { "Key1", "Value1" },
                { "Key2", "2" },
                { "Key3", "true" },
                { "Key4", "DateTime.Now" },
                { "Key5", "new TimeSpan()" }
            };
            ScopedNode subTask = new ScopedNode(taskScope1);
            subTask.ParentId = subScope.Id;

            var rootScope = new Scope();
            rootScope.Id = Guid.Parse("00000000-0000-0000-0000-000000000001");

            ScopedNode workflowScopeNode = new ScopedNode(workflowScope);
            ScopedNode taskScope1Node = new ScopedNode(taskScope1);
            ScopedNode taskScope2Node = new ScopedNode(taskScope2);

            subScope.ParentId = rootScope.Id;
            workflowScopeNode.ParentId = rootScope.Id;
            taskScope1Node.ParentId = rootScope.Id;
            taskScope2Node.ParentId = rootScope.Id;
            #endregion
            JsonSerializerOptions options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(
                new TestData()
                {
                    Nodes = new List<Node> { taskScope1, taskScope2, workflowInput, workflowOutput, workflowScope },
                    ScopedElements =
                        new List<ScopedElement>
                            {
                                subTask,
                                subScope,
                                rootScope,
                                workflowScopeNode,
                                taskScope1Node,
                                taskScope2Node
                            },
                    Connectors =
                        new List<NodeConnector>()
                            {
                                input1,
                                output1,
                                input2,
                                output2,
                                flowIn1,
                                flowIn2,
                                flowOut1,
                                flowOut2,
                                flowOut3,
                                flowIn3
                            },
                    Connections = new List<NodeConnection>() { connection },
                    WorkflowRelations =
                        new List<WorkflowRelation>()
                            {
                                new WorkflowRelation() { WorkflowId = workflowScope.Id, NodeId = taskScope1.Id },
                                new WorkflowRelation() { WorkflowId = workflowScope.Id, NodeId = taskScope2.Id },
                                new WorkflowRelation() { WorkflowId = workflowScope.Id, NodeId = workflowInput.Id },
                                new WorkflowRelation() { WorkflowId = workflowScope.Id, NodeId = workflowOutput.Id },
                            },
                },
                options);
        }

        #endregion
    }
}
