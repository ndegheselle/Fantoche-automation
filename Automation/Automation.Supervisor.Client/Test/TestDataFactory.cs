using Automation.Shared.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Automation.Supervisor.Client.Test
{
    // Separated like it would be in a relationnal database
    public struct TestData
    {
        public List<INode> Nodes { get; set; }

        public List<ScopedElement> ScopedElements { get; set; }

        public List<TaskConnector> Connectors { get; set; }

        public List<NodeConnection> Connections { get; set; }

        public List<WorkflowRelation> WorkflowRelations { get; set; }
    }

    public static class TestDataFactory
    {
        #region Debug
        public static TestData LoadTestData()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "Automation.Supervisor.Client.Resources.test.json";

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                string jsonFile = reader.ReadToEnd(); //Make string equal to full file
                return JsonSerializer.Deserialize<TestData>(jsonFile);
            }
        }

        public static string CreateTestData()
        {
            TaskNode taskScope1 = new TaskNode() { Name = "Task 1" };
            var flowIn1 = new NodeInput() { ParentId = taskScope1.Id, Type = EnumTaskConnectorType.Flow };
            var flowOut1 = new NodeOutput() { ParentId = taskScope1.Id, Type = EnumTaskConnectorType.Flow };
            var input1 = new NodeInput() { Name = "Input 2", ParentId = taskScope1.Id };
            var output1 = new NodeOutput() { Name = "Output 2", ParentId = taskScope1.Id };

            TaskNode taskScope2 = new TaskNode() { Name = "Task 2", };
            var flowIn2 = new NodeInput() { ParentId = taskScope2.Id, Type = EnumTaskConnectorType.Flow };
            var flowOut2 = new NodeOutput() { ParentId = taskScope2.Id, Type = EnumTaskConnectorType.Flow };
            var input2 = new NodeInput() { Name = "Input 1", ParentId = taskScope2.Id };
            var output2 = new NodeOutput() { Name = "Output 1", ParentId = taskScope2.Id };

            WorkflowInputNode workflowInput = new WorkflowInputNode() { Name = "Start", };
            var flowOut3 = new NodeOutput() { ParentId = workflowInput.Id, Type = EnumTaskConnectorType.Flow };

            WorkflowInputNode workflowOutput = new WorkflowInputNode() { Name = "End", };
            var flowIn3 = new NodeInput() { ParentId = workflowOutput.Id, Type = EnumTaskConnectorType.Flow };

            WorkflowNode workflowScope = new WorkflowNode() { Name = "Workflow 1", };

            var connection = new NodeConnection(workflowScope, output2, input1);

            #region Scoped elements
            Scope subScope = new Scope() { Name = "SubScope 1", };
            subScope.Context = new Dictionary<string, string>()
            { { "Key1", "Value1" }, { "Key2", "Value2" }, { "Key3", "Value3" }, };
            ScopedTask subTask = new ScopedTask(taskScope1);
            subTask.ParentId = subScope.Id;

            var rootScope = new Scope();
            rootScope.Id = Guid.Parse("00000000-0000-0000-0000-000000000001");

            ScopedTask workflowScopeNode = new ScopedTask(workflowScope);
            ScopedTask taskScope1Node = new ScopedTask(taskScope1);
            ScopedTask taskScope2Node = new ScopedTask(taskScope2);

            subScope.ParentId = rootScope.Id;
            workflowScopeNode.ParentId = rootScope.Id;
            taskScope1Node.ParentId = rootScope.Id;
            taskScope2Node.ParentId = rootScope.Id;
            #endregion

            JsonSerializerOptions options = new JsonSerializerOptions { WriteIndented = true };
            return JsonSerializer.Serialize(
                new TestData()
                {
                    Nodes = new List<INode> { taskScope1, taskScope2, workflowInput, workflowOutput, workflowScope },
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
