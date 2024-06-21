using Automation.Base;
using System.Collections.ObjectModel;

namespace Automation.Supervisor.Repositories
{
    public class ScopeRepository
    {
        private readonly Scope _rootScope;
        private readonly List<Node> _nodes;

        public ScopeRepository()
        {
            TaskNode taskScope1 = new TaskNode()
            {
                Name = "Task 1",
                Inputs = new ObservableCollection<NodeConnector>() { new NodeConnector() { Name = "Input 1" }, },
                Outputs = new ObservableCollection<NodeConnector>() { new NodeConnector() { Name = "Output 1" }, },
            };

            TaskNode taskScope2 = new TaskNode()
            {
                Name = "Task 2",
                Inputs = new ObservableCollection<NodeConnector>() { new NodeConnector() { Name = "Input 1" }, },
                Outputs = new ObservableCollection<NodeConnector>() { new NodeConnector() { Name = "Output 1" }, },
            };

            WorkflowNode workflowScope = new WorkflowNode() { Name = "Workflow 1", };
            workflowScope.Nodes.Add(taskScope1);
            workflowScope.Nodes.Add(taskScope2);

            workflowScope.Connections.Add(new NodeConnection(taskScope2.Outputs[0], taskScope1.Inputs[0]));

            Scope subScope = new Scope() { Name = "SubScope 1", };
            subScope.AddChild(taskScope1);

            Scope mainScope = new Scope() { Name = "Scope 1", };
            mainScope.AddChild(subScope);
            mainScope.AddChild(workflowScope);
            mainScope.AddChild(taskScope2);

            _rootScope = new Scope();
            _rootScope.AddChild(mainScope);

            _nodes = new List<Node>();
            _nodes.Add(taskScope1);
            _nodes.Add(taskScope2);
            _nodes.Add(workflowScope);
            _nodes.Add(subScope);
            _nodes.Add(_rootScope);
        }

        public Scope GetRootScope() { return _rootScope; }

        public IEnumerable<Node> GetScopeChildrens(Guid id)
        {
            return _nodes.Where(n => n.ParentId == id);
        }
    }
}
