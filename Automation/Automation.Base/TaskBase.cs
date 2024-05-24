using Automation.Plugins.Base;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Automation.Base
{
    public class TaskBase : ITask
    {
        public dynamic? Context { get; set; }
        public event EventHandler<TaskProgress>? Progress;

        public Dictionary<string, Type> InputsDefinition { get; protected set; }
        public Dictionary<string, Type> OutputsDefinition { get; protected set; }

        public Task<EnumTaskStatus> Start(Dictionary<string, dynamic> inputs)
        {
            if (!ValidateInputs(inputs))
                return Task.FromResult(EnumTaskStatus.Failed);
            
            return Task.FromResult(EnumTaskStatus.Completed);
        }

        protected virtual bool ValidateInputs(Dictionary<string, dynamic> inputs)
        {
            foreach (var definition in InputsDefinition)
            {
                // If the input is not nullable and the input is not provided, return false
                if (!inputs.ContainsKey(definition.Key) && Nullable.GetUnderlyingType(definition.Value) == null)
                {
                    return false;
                }
                // Incorect type
                else if (inputs.ContainsKey(definition.Key) && !definition.Value.IsInstanceOfType(inputs[definition.Key]))
                {
                    return false;
                }
            }
            return true;
        }
    }

    public class TaskWorkflow : TaskBase
    {
        public List<TaskBase> Tasks { get; set; }
    }

    public class TaskGraph : TaskBase
    {
        public List<TaskBase> Tasks { get; set; }
        public List<TaskLink> Links { get; set; }
    }

    public class TaskLink
    {
        public string SourceOutput { get; set; }
        public TaskBase Source { get; set; }
        public string TargetInput { get; set; }
        public TaskBase Target { get; set; }
    }
}
