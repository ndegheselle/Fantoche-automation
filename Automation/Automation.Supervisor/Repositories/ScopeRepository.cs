using Automation.Base;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Xml.Linq;

namespace Automation.Supervisor.Repositories
{
    public class ScopeRepository
    {
        // XXX : should return Scope with only one depth of children
        public ScopeRepository()
        {
        }

        public Scope GetRootScope() {
            return (Scope)GetNode(Guid.Parse("00000000-0000-0000-0000-000000000001"));
        }

        public Node? GetNode(Guid id)
        {
            var nodes = LoadTestData();
            Node? node = nodes.FirstOrDefault(x => x.Id == id);

            if (node == null)
                return null;

            if (node is Scope scope)
            {
                foreach (Node child in nodes.Where(x => x.ParentId == scope.Id))
                {
                    scope.AddChild(child);
                }
            }

            return node;
        }

        #region Debug

        private List<Node> LoadTestData()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "Automation.Supervisor.Resources.test.json";

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                string jsonFile = reader.ReadToEnd(); //Make string equal to full file
                return JsonSerializer.Deserialize<List<Node>>(jsonFile);
            }
        }

        private void CreateTestNodes()
        {
            TaskNode taskScope1 = new TaskNode()
            {
                Name = "Task 1",
                Inputs = new List<NodeConnector>() { new NodeConnector() { Name = "Input 1" }, },
                Outputs = new List<NodeConnector>() { new NodeConnector() { Name = "Output 1" }, },
            };

            TaskNode taskScope2 = new TaskNode()
            {
                Name = "Task 2",
                Inputs = new List<NodeConnector>() { new NodeConnector() { Name = "Input 1" }, },
                Outputs = new List<NodeConnector>() { new NodeConnector() { Name = "Output 1" }, },
            };

            WorkflowNode workflowScope = new WorkflowNode() { Name = "Workflow 1", };
            workflowScope.Nodes.Add(taskScope1);
            workflowScope.Nodes.Add(taskScope2);

            workflowScope.Connections.Add(new NodeConnection(taskScope2.Outputs[0], taskScope1.Inputs[0]));

            Scope subScope = new Scope() { Name = "SubScope 1", };
            taskScope1.ParentId = subScope.Id;

            var rootScope = new Scope();
            rootScope.Id = Guid.Parse("00000000-0000-0000-0000-000000000001");
            workflowScope.ParentId = rootScope.Id;
            subScope.ParentId = rootScope.Id;
            taskScope2.ParentId = rootScope.Id;

            var nodes = new List<Node>
            {
                taskScope1,
                taskScope2,
                workflowScope,
                subScope,
                rootScope
            };

            JsonSerializerOptions options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            string json = JsonSerializer.Serialize(nodes, options);
        }

        #endregion
    }
}
