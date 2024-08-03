using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Automation.Shared.Data
{
    public enum EnumTaskConnectorType
    {
        Data,
        Flow
    }

    public enum EnumTaskConnectorDirection
    {
        In,
        Out
    }

    public interface INode
    {
        Guid Id { get; set; }
        string Name { get; set; }
    }

    public interface INodeGroup : INode
    {
        Size Size { get; set; }
        Point Location { get; set; }
    }

    public interface ITaskNode : INode
    {
        Guid ScopeId { get; set; }
        IList<ITaskConnector> Connectors { get; }
    }

    public interface ITaskConnector
    {
        EnumTaskConnectorType Type { get; set; }
        EnumTaskConnectorDirection Direction { get; set; }
        Guid Id { get; set; }
        string Name { get; set; }
        Guid ParentId { get; set; }
    }
}
