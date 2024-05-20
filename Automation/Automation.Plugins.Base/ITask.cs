using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Automation.Plugins.Base
{
    public enum EnumTaskStatus
    {
        Completed,
        Failed
    }

    public interface ITask
    {
        public dynamic? Context { get; set; }
        public Task<EnumTaskStatus> Start(Dictionary<string, dynamic> inputs);
    }
}
