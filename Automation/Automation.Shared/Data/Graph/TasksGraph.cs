using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using Automation.Shared.Data.Scoped;
using NJsonSchema;

namespace Automation.Shared.Data.Graph
{
    public class TasksGraph
    {
        public ObservableCollection<GraphConnection> Connections { get; set; } = [];
        public ObservableCollection<GraphNode> Nodes { get; set; } = [];

        public bool IsRefreshed { get; private set; } = false;

        [JsonIgnore]
        public GraphExecutionContext Execution { get; private set; }

        public TasksGraph()
        {
            Execution = new GraphExecutionContext(this);
        }

        /// <summary>
        /// Refresh parent and object references between TaskNode, Connection and Connectors.
        /// Simplify the graph resolution.
        /// </summary>
        /// <param name="force">Force the refresh even if the graph is already refreshed.</param>
        public void Refresh(Dictionary<Guid, BaseAutomationTask>? tasks = null, bool force = false)
        {
            if (IsRefreshed && !force)
                return;

            var connectors = new Dictionary<Guid, GraphConnector>();
            foreach (GraphNode node in Nodes)
            {
                if (node is not BaseGraphTask taskNode)
                    continue;

                // Refresh node target task
                if (tasks != null)
                {
                    if (taskNode.TaskId == AutomationControl.StartTask.Id)
                        taskNode.AutomationTask = AutomationControl.StartTask;
                    if (taskNode.TaskId == AutomationControl.EndTask.Id)
                        taskNode.AutomationTask = AutomationControl.EndTask;
                    else
                        taskNode.AutomationTask = tasks[taskNode.TaskId];
                }

                // Refresh inputs parent
                foreach (GraphConnector connector in taskNode.Inputs)
                {
                    connectors.Add(connector.Id, connector);
                    connector.Parent = taskNode;
                }

                // Refresh output parent
                foreach (GraphConnector connector in taskNode.Outputs)
                {
                    connectors.Add(connector.Id, connector);
                    connector.Parent = taskNode;
                }
            }

            // Set connections with corresponding connectors
            foreach (GraphConnection connection in Connections)
            {
                GraphConnector source = connectors[connection.SourceId];
                GraphConnector target = connectors[connection.TargetId];
                connection.Connect(source, target);
            }

            IsRefreshed = true;
        }

        #region Nodes

        public IEnumerable<GraphControl> GetStartNodes() => Nodes.OfType<GraphControl>().Where(x => x.IsStart());
        public IEnumerable<GraphControl> GetEndNodes() => Nodes.OfType<GraphControl>().Where(x => x.IsEnd());

        public string GetUniqueNodeName(string nodeName)
        {
            string uniqueName = nodeName;
            int count = 1;

            // Check if the name exists; if so, append a number and try again
            while (Nodes.Any(x => x.Name == uniqueName))
            {
                uniqueName = $"{nodeName} {count}";
                count++;
            }

            return uniqueName;
        }

        #endregion

        #region Connections

        /// <summary>
        /// Connect two tasks with their first connectors.
        /// </summary>
        /// <param name="task"></param>
        /// <param name="task2"></param>
        public void Connect(BaseGraphTask task, BaseGraphTask task2)
        {
            Connections.Add(new GraphConnection(task.Outputs.First(), task2.Inputs.First()));
        }

        /// <summary>
        /// Get all previous tasks.
        /// </summary>
        /// <param name="task">Task to get the previous tasks from</param>
        /// <returns></returns>
        public IEnumerable<BaseGraphTask> GetPrevious(BaseGraphTask task)
        {
            return GetInputsConnectionsFrom(task).Select(x => x.Source!.Parent!);
        }

        /// <summary>
        /// Get all next tasks paired with the source connector they are reachable from.
        /// </summary>
        /// <param name="task">Task to get the previous tasks from</param>
        public IEnumerable<GraphSource> GetNext(BaseGraphTask task)
        {
            return GetOutputsConnectionsFrom(task)
                .Select(c => new GraphSource(c.Target!.Parent!, c.Source!));
        }

        public bool WithMultipleInputsConnections(BaseGraphTask task)
        {
            return GetInputsConnectionsFrom(task).Count() > 1;
        }

        /// <summary>
        /// Get all the connections linked to a task.
        /// </summary>
        /// <param name="task"></param>
        /// <returns></returns>
        public IEnumerable<GraphConnection> GetConnectionsFrom(BaseGraphTask task)
        {
            List<GraphConnection> connections = [];
            connections.AddRange(GetInputsConnectionsFrom(task));
            connections.AddRange(GetOutputsConnectionsFrom(task));
            return connections;
        }

        /// <summary>
        /// Get all the input connections linked to a task.
        /// </summary>
        /// <returns></returns>
        private IEnumerable<GraphConnection> GetInputsConnectionsFrom(BaseGraphTask task)
        {
            List<GraphConnection> connections = [];
            foreach (GraphConnector input in task.Inputs)
                connections.AddRange(GetConnectionsFrom(input));
            return connections;
        }

        /// <summary>
        /// Get all the output connections linked to a task.
        /// </summary>
        /// <returns></returns>
        private IEnumerable<GraphConnection> GetOutputsConnectionsFrom(BaseGraphTask task)
        {
            List<GraphConnection> connections = [];
            foreach (GraphConnector input in task.Outputs)
                connections.AddRange(GetConnectionsFrom(input));
            return connections;
        }

        /// <summary>
        /// Get all the connections linked to a connector.
        /// </summary>
        /// <param name="connector"></param>
        /// <returns></returns>
        public IEnumerable<GraphConnection> GetConnectionsFrom(GraphConnector connector)
        {
            return Connections.Where(x => x.SourceId == connector.Id || x.TargetId == connector.Id);
        }

        #endregion
    }
}