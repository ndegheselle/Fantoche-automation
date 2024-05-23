using Automation.Plugins.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Automation.Plugins.Flow
{
    public class WaitAll : ITask
    {
        public dynamic? Context { get; set; }
        public event EventHandler<TaskProgress>? Progress;

        public Task<EnumTaskStatus> Start(Dictionary<string, dynamic> inputs)
        {
            throw new NotImplementedException();
        }
    }
}
