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

    public interface INamed
    {
        Guid Id { get; }
        string Name { get; }
    }

    public interface ITaskNode : INamed
    {
        Guid ScopeId { get; set; }
        IEnumerable<ITaskConnector> Connectors { get; }
    }

    public interface ITaskConnector
    {
        Guid Id { get; }
        string Name { get; set; }
        EnumTaskConnectorType Type { get; set; }
        EnumTaskConnectorDirection Direction { get; set; }
    }
}
