using System.Collections.ObjectModel;
using System.Drawing;
using System.Text.Json.Serialization;
using System.Windows;

namespace Automation.Shared.Data
{
    #region Interfaces

    public interface IWorkflowNode : ITaskNode
    {
        IList<ITaskConnection> Connections { get; }
        IList<INodeGroup> Groups { get; }
        IList<ITaskNode> Tasks { get; }
        IList<IWorkflowRelation> Relations { get; }
    }

    public interface IWorkflowRelation
    {
        Guid WorkflowId { get; set; }
        Guid TaskId { get; set; }
        Point TaskPosition { get; set; }
    }

    public interface ITaskConnection
    {
        Guid ParentId { get; set; }
        Guid SourceId { get; set; }
        Guid TargetId { get; set; }
    }

    #endregion

    public class WorkflowNode : TaskNode
    {
        public List<TaskConnection> Connections { get; set; } = [];
        public List<NodeGroup> Groups { get; set; } = [];
        public List<TaskNode> Tasks { get; set; } = [];
        public Dictionary<Guid, WorkflowRelation> Relations { get; set; } = [];
    }

    public class WorkflowRelation
    {
        public Guid WorkflowId { get; set; }
        public Guid TaskId { get; set; }
        public Point TaskPosition { get; set; }
    }

    public partial class TaskConnection
    {
        public Guid ParentId { get; set; }

        public Guid SourceId { get; set; }
        public Guid TargetId { get; set; }

        // Deserialization
        public TaskConnection() { }

        public TaskConnection(WorkflowNode parent, TaskConnector source, TaskConnector target)
        {
            ParentId = parent.Id;
            SourceId = source.Id;
            TargetId = target.Id;
        }
    }
}