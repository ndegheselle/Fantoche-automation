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

        public Dictionary<string, TaskEndpoint> Inputs { get; set; } = [];

        public Dictionary<string, TaskEndpoint> Outputs { get; set; } = [];

        public virtual Task<bool> Start()
        {
            if (!ValidateInputs(Inputs))
                return Task.FromResult(false);
            return Task.FromResult(true);
        }

        protected virtual bool ValidateInputs(Dictionary<string, TaskEndpoint> inputs)
        {
            foreach (var input in inputs)
            {
                if (!input.Value.IsValid())
                    return false;
            }
            return true;
        }
    }
}
