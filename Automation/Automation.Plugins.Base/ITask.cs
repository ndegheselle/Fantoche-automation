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

    public class TaskEndpoint
    {
        public Type Type { get; set; }
        public dynamic Data { get; set; }
    }

    public interface ITask
    {
        public event EventHandler<TaskProgress>? Progress;

        public Dictionary<string, TaskEndpoint> Inputs { get; }
        public Dictionary<string, TaskEndpoint> Outputs { get; }

        public dynamic? Context { get; set; }
        EnumTaskStatus Status { get; }

        public Task<EnumTaskStatus> Start();
    }
}
