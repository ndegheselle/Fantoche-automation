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

        public Dictionary<string, TaskEndpoint> Inputs { get; protected set; }
        public Dictionary<string, TaskEndpoint> Outputs { get; protected set; }


        public virtual Task<EnumTaskStatus> Start()
        {
            if (!ValidateInputs())
                return Task.FromResult(EnumTaskStatus.Failed);
            
            foreach (var input in inputs)
                Inputs[input.Key].Data = inputs[input.Key];

            return Task.FromResult(EnumTaskStatus.Completed);
        }

        protected virtual bool ValidateInputs()
        {
            foreach (var input in Inputs)
            {
                // If the input is not nullable and the input is not provided, return false
                if (!inputs.ContainsKey(input.Key) && Nullable.GetUnderlyingType(input.Value.Type) == null)
                {
                    return false;
                }
                // Incorect type
                else if (inputs.ContainsKey(input.Key) && !input.Value.Type.IsInstanceOfType(inputs[input.Key]))
                {
                    return false;
                }
            }
            return true;
        }

        protected virtual bool CheckErrors()
        {
            return true;
        }
    }
}
