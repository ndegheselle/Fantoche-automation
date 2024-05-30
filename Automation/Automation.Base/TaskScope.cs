using Automation.Plugins.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Automation.Base
{
    public class TaskScope
    {
        public dynamic? Context { get; set; }
        public string Name { get; set; }
        // Can either be a ITask or a TaskScope
        public List<object> Childrens { get; set; } = new List<object>();
    }
}
