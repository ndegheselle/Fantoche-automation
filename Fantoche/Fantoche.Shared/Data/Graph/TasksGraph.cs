using System.Collections.ObjectModel;
using Fantoche.Shared.Data.Scoped;
using NJsonSchema;

namespace Fantoche.Shared.Data.Graph
{
    public class TasksGraph
    {
        public ObservableCollection<GraphConnection> Connections { get; set; } = [];
        public ObservableCollection<GraphNode> Nodes { get; set; } = [];
        public bool IsRefreshed { get; private set; }

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

                // Refresh node target task. The control tasks are hard coded : they are known even
                // when no task is given, so a graph refreshed to be displayed still knows which of
                // its nodes are passing through.
                if (AutomationControl.Get(taskNode.TaskId) is AutomationControl control)
                    taskNode.AutomationTask = control;
                else if (tasks != null && tasks.TryGetValue(taskNode.TaskId, out BaseAutomationTask? task))
                    taskNode.AutomationTask = task;

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

                if (taskNode.UserInterface is GraphConnector userInterface)
                {
                    connectors.Add(userInterface.Id, userInterface);
                    userInterface.Parent = taskNode;
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

        /// <summary>
        /// Whether [node] can join the graph : a workflow is entered once and left once, so it holds
        /// a single start and a single end. Anything else can be added as many times as wanted.
        /// </summary>
        public bool CanAdd(GraphNode node)
        {
            if (node is not GraphControl control)
                return true;
            if (control.IsStart())
                return !GetStartNodes().Any();
            if (control.IsEnd())
                return !GetEndNodes().Any();
            return true;
        }

        /// <summary>
        /// What is wrong with the shape of the graph, empty when it holds up. An editor refuses the
        /// edition beforehand (see <see cref="CanAdd"/>), this is what a graph reaching the storage
        /// from anywhere else is checked against. A graph being built has no start nor end yet, only
        /// holding several of them is refused.
        /// </summary>
        public List<string> GetStructureErrors()
        {
            List<string> errors = [];

            int starts = GetStartNodes().Count();
            if (starts > 1)
                errors.Add($"A workflow can only have one start node, {starts} starts found.");

            int ends = GetEndNodes().Count();
            if (ends > 1)
                errors.Add($"A workflow can only have one end node, {ends} ends found.");

            return errors;
        }

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
        /// <exception cref="InvalidOperationException">The connection isn't allowed, see <see cref="CanConnect"/>.</exception>
        public void Connect(BaseGraphTask task, BaseGraphTask task2)
        {
            GraphConnector source = task.Outputs.First();
            GraphConnector target = task2.Inputs.First();
            if (!CanConnect(source, target))
                throw new InvalidOperationException($"'{task.Name}' can't be connected to '{task2.Name}'.");

            Connections.Add(new GraphConnection(source, target));
        }

        /// <summary>
        /// Attach [page] under [task] : the user is asked the parameters of the task through its page
        /// before it runs.
        /// </summary>
        /// <exception cref="InvalidOperationException">The page can't be attached, see <see cref="CanConnect"/>.</exception>
        public void Attach(BaseGraphTask task, GraphUserInterface page)
        {
            if (task.UserInterface == null || page.UserInterface == null || !CanConnect(task.UserInterface, page.UserInterface))
                throw new InvalidOperationException(GetAttachError(task, page) ?? $"'{page.Name}' can't be attached to '{task.Name}'.");

            Connections.Add(new GraphConnection(task.UserInterface, page.UserInterface));
        }

        /// <summary>
        /// Whether [source] can be connected to [target] : an output and an input held by two
        /// different nodes of this graph, not already connected together.
        /// <para>
        /// The rule lives here rather than in whichever editor offers the connection, the executor
        /// walking the graph with no cycle guard : a task connected to itself hangs it.
        /// </para>
        /// </summary>
        public bool CanConnect(GraphConnector source, GraphConnector target)
        {
            if (FindNode(x => OptionalList(x.UserInterface), source) != null)
                return CanAttach(source, target);

            // Resolved by id rather than through GraphConnector.Parent, which is only set once
            // Refresh() has run.
            BaseGraphTask? sourceNode = FindNode(x => x.Outputs, source);
            BaseGraphTask? targetNode = FindNode(x => x.Inputs, target);

            // Unknown connectors, or dragged the wrong way around : an output only reaches an input.
            if (sourceNode == null || targetNode == null || sourceNode == targetNode)
                return false;

            return !Connections.Any(x => x.SourceId == source.Id && x.TargetId == target.Id);
        }

        /// <summary>
        /// Whether the "ask the user" node plugged by [plug] can be attached to the task holding the
        /// slot [slot] : it shows the page of that task. A task is asked by a single node, and a node
        /// asks for a single task.
        /// </summary>
        private bool CanAttach(GraphConnector slot, GraphConnector plug)
        {
            BaseGraphTask? task = FindNode(x => OptionalList(x.UserInterface), slot);
            BaseGraphTask? page = FindNode(x => OptionalList(x.UserInterface), plug);

            return task != null && page != null && GetAttachError(task, page) == null;
        }

        /// <summary>
        /// Why [page] can't be attached under [task], null when it can (see <see cref="CanAttach"/>).
        /// The page doesn't have to be in the graph yet : one added under its task is checked before
        /// it is added.
        /// </summary>
        public string? GetAttachError(BaseGraphTask task, BaseGraphTask page)
        {
            if (page is not GraphUserInterface || page.UserInterface == null)
                return $"'{page.Name}' doesn't ask the user, only an \"ask the user\" node does.";
            if (task is GraphUserInterface || task.UserInterface == null)
                return $"'{task.Name}' has no page to ask the user with.";
            if (Connections.Any(x => x.SourceId == task.UserInterface.Id))
                return $"'{task.Name}' already asks the user.";
            if (Connections.Any(x => x.TargetId == page.UserInterface.Id))
                return $"'{page.Name}' is already attached to another task.";
            if (task.TaskId != page.TaskId)
                return $"'{page.Name}' shows the page of another task than '{task.Name}'.";
            return null;
        }

        private static IEnumerable<GraphConnector> OptionalList(GraphConnector? connector)
            => connector == null ? [] : [connector];

        /// <summary>
        /// The node asking the user for the parameters of [task] before it runs, null when it runs
        /// unattended.
        /// </summary>
        public GraphUserInterface? GetUserInterfaceOf(BaseGraphTask task)
        {
            if (task.UserInterface == null || task is GraphUserInterface)
                return null;

            GraphConnection? attachment = Connections.FirstOrDefault(x => x.SourceId == task.UserInterface.Id);
            if (attachment == null)
                return null;

            return Nodes.OfType<GraphUserInterface>().FirstOrDefault(x => x.UserInterface?.Id == attachment.TargetId);
        }

        /// <summary>The task [page] asks the parameters of, null while it is attached to none.</summary>
        public BaseGraphTask? GetTaskAskedBy(GraphUserInterface page)
        {
            if (page.UserInterface == null)
                return null;

            GraphConnection? attachment = Connections.FirstOrDefault(x => x.TargetId == page.UserInterface.Id);
            if (attachment == null)
                return null;

            return Nodes.OfType<BaseGraphTask>().FirstOrDefault(x => x.UserInterface?.Id == attachment.SourceId);
        }

        /// <summary>
        /// The node of the graph holding [connector] in the collection given by [connectors].
        /// </summary>
        private BaseGraphTask? FindNode(
            Func<BaseGraphTask, IEnumerable<GraphConnector>> connectors,
            GraphConnector connector)
        {
            return Nodes
                .OfType<BaseGraphTask>()
                .FirstOrDefault(x => connectors(x).Any(c => c.Id == connector.Id));
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
        /// What [task] actually stands for : a task passing through produces nothing of its own, so
        /// the branches leading to it are what the next ones read.
        /// </summary>
        public IEnumerable<BaseGraphTask> GetEffective(BaseGraphTask task)
        {
            if (task.AutomationTask?.Settings.IsPassingThrough != true)
                return [task];
            return GetPrevious(task).SelectMany(GetEffective);
        }

        /// <summary>
        /// Get all next tasks paired with the source connector they are reachable from.
        /// </summary>
        /// <param name="task">Task to get the previous tasks from</param>
        public IEnumerable<GraphSource> GetNext(BaseGraphTask task)
        {
            return GetOutputsConnectionsFrom(task)
                .Select(c => new GraphSource(c.Target!.Parent!, c.Source!, c));
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