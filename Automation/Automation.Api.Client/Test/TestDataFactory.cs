using Automation.Shared.Data;
using System.Text.Json;

namespace Automation.Api.Client.Test
{
    // Separated like it would be in a relationnal database
    public struct TestData
    {
        public List<TaskNode> Tasks { get; set; }

        public List<Scope> Scopes { get; set; }

        public List<TaskConnector> Connectors { get; set; }

        public List<TaskConnection> Connections { get; set; }

        public List<WorkflowRelation> WorkflowRelations { get; set; }
    }

    public static class TestDataFactory
    {
        public static TestData Data = CreateTestData();

        public static TestData CreateTestData()
        {
            TaskNode taskScope1 = new TaskNode() { Name = "Task 1", Id = Guid.NewGuid() };
            var flowIn1 = new TaskConnector() { ParentId = taskScope1.Id, Type = EnumTaskConnectorType.Flow, Direction = EnumTaskConnectorDirection.In };
            var flowOut1 = new TaskConnector() { ParentId = taskScope1.Id, Type = EnumTaskConnectorType.Flow, Direction = EnumTaskConnectorDirection.Out };
            var input1 = new TaskConnector() { Name = "Input 2", ParentId = taskScope1.Id, Direction = EnumTaskConnectorDirection.In };
            var output1 = new TaskConnector() { Name = "Output 2", ParentId = taskScope1.Id, Direction = EnumTaskConnectorDirection.Out };

            TaskNode taskScope2 = new TaskNode() { Name = "Task 2", Id = Guid.NewGuid() };
            var flowIn2 = new TaskConnector() { ParentId = taskScope2.Id, Type = EnumTaskConnectorType.Flow, Direction = EnumTaskConnectorDirection.In };
            var flowOut2 = new TaskConnector() { ParentId = taskScope2.Id, Type = EnumTaskConnectorType.Flow, Direction = EnumTaskConnectorDirection.Out };
            var input2 = new TaskConnector() { Name = "Input 1", ParentId = taskScope2.Id, Direction = EnumTaskConnectorDirection.In };
            var output2 = new TaskConnector() { Name = "Output 1", ParentId = taskScope2.Id, Direction = EnumTaskConnectorDirection.Out };

            TaskNode workflowInput = new TaskNode() { Name = "Start", Id = Guid.NewGuid() };
            var flowOut3 = new TaskConnector() { ParentId = workflowInput.Id, Type = EnumTaskConnectorType.Flow, Direction = EnumTaskConnectorDirection.Out };

            TaskNode workflowOutput = new TaskNode() { Name = "End", Id = Guid.NewGuid() };
            var flowIn3 = new TaskConnector() { ParentId = workflowOutput.Id, Type = EnumTaskConnectorType.Flow, Direction = EnumTaskConnectorDirection.In };

            WorkflowNode workflowScope = new WorkflowNode() { Name = "Workflow 1", Id = Guid.NewGuid() };

            var connection = new TaskConnection(workflowScope, output2, input1);

            Scope subScope = new Scope() { Name = "SubScope 1", Id = Guid.NewGuid() };
            subScope.Context = new Dictionary<string, string>()
            { { "Key1", "Value1" }, { "Key2", "Value2" }, { "Key3", "Value3" }, };

            var rootScope = new Scope();
            rootScope.Id = Guid.Parse("00000000-0000-0000-0000-000000000001");
            subScope.ParentId = rootScope.Id;

            taskScope1.ScopeId = subScope.Id;
            taskScope2.ScopeId = rootScope.Id;
            workflowScope.ScopeId = rootScope.Id;

            JsonSerializerOptions options = new JsonSerializerOptions { WriteIndented = true };
            return new TestData()
            {
                Tasks = new List<TaskNode> { taskScope1, taskScope2, workflowInput, workflowOutput, workflowScope },
                Scopes =
                        new List<Scope>
                            {
                                subScope,
                                rootScope
                            },
                Connectors =
                        new List<TaskConnector>()
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
                Connections = new List<TaskConnection>() { connection },
                WorkflowRelations =
                        new List<WorkflowRelation>()
                            {
                                new WorkflowRelation() { WorkflowId = workflowScope.Id, TaskId = taskScope1.Id },
                                new WorkflowRelation() { WorkflowId = workflowScope.Id, TaskId = taskScope2.Id },
                                new WorkflowRelation() { WorkflowId = workflowScope.Id, TaskId = workflowInput.Id },
                                new WorkflowRelation() { WorkflowId = workflowScope.Id, TaskId = workflowOutput.Id },
                            },
            };
        }
    }

}
