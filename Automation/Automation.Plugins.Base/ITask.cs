using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Automation.Plugins.Base
{
    public class TaskEndpoint
    {
        public Type Type { get; set; }
        public ITask Parent { get; set; }
        public object? Data { get; set; }

        public TaskEndpoint(Type type, ITask parent)
        {
            Type = type;
            Parent = parent;
        }

        public bool IsValid()
        {
            if (Nullable.GetUnderlyingType(Type) == null && Data == null)
                return false;
            return Data?.GetType().IsInstanceOfType(Type) == true;
        }
    }

    public interface ITask
    {
        public Dictionary<string, TaskEndpoint> Inputs { get; set; }
        public Dictionary<string, TaskEndpoint> Outputs { get; set; }

        public dynamic? Context { get; set; }
        public Task<bool> Start();
    }
}
