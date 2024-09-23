using Automation.Shared.Base;
using Automation.Shared.Packages;
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
        Guid Id { get; set; }
        string Name { get; }
    }

    public interface ITaskNode : INamed
    {
        PackageInfos? Package { get; set; }
        Guid ScopeId { get; set; }
        IEnumerable<ITaskConnector> Inputs { get; }
        IEnumerable<ITaskConnector> Outputs { get; }
    }

    public interface ITaskConnector
    {
        Guid Id { get; }
        string Name { get; set; }
        EnumTaskConnectorType Type { get; set; }
        EnumTaskConnectorDirection Direction { get; set; }
    }
}
