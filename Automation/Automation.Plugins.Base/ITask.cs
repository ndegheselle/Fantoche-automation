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

    public struct TaskProgress
    {
        public EnumTaskStatus Status { get; set; }
        public string Message { get; set; }
    }

    public interface ITask
    {
        public event EventHandler<TaskProgress>? Progress;

        public dynamic? Context { get; set; }
        public Task<EnumTaskStatus> Start(Dictionary<string, dynamic> inputs);
    }
}
