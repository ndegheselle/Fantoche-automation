using Automation.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Automation.Worker
{
    public class Worker
    {
        public TaskBase Target { get; set; }

        public Worker(TaskScoped target)
        {
            Target = target;
        }

        public void Start()
        {

        }
    }
}
