using Automation.Plugins.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Automation.Base
{
    public class TaskGraph : TaskBase
    {
        public List<ITask> Tasks { get; set; } = [];

        public List<TaskLink> Links { get; set; } = [];

        public override async Task<bool> Start()
        {
            if (!ValidateInputs(Inputs))
                return false;
            var startingTask = GetStartingTask();

            foreach (var input in Inputs)
                startingTask.Inputs.Add(input.Key, input.Value);

            return await ExecuteNode(startingTask);
        }
        
        private ITask GetStartingTask()
        {
            IEnumerable<ITask> startingTasks = Tasks.Where(t => t is GraphFlowStart);
            if (startingTasks.Count() == 0)
                throw new Exception("No starting task found");
            if (startingTasks.Count() > 1)
                throw new Exception("Multiple starting tasks found");

            return startingTasks.First();
        }

        private async Task<bool> ExecuteNode(ITask node)
        {
            bool result = await node.Start();
            // Get the current outputs
            this.Outputs = node.Outputs;

            if (!result)
                return false;

            foreach (TaskLink link in Links.Where(l => l.From.Parent == node))
            {
                link.Status = true;
                // Ignore the link if the target node doesn't have all its links ready
                if (Links.Where(l => l.From.Parent == link.To).Any(l => l.Status == false))
                    continue;
                result = await ExecuteNode(link.To.Parent);
                if (!result)
                    return false;
            }
            return true;
        }
    }

    public class TaskLink
    {
        public TaskEndpoint From { get; set; }
        public TaskEndpoint To { get; set; }

        private bool? _Status;
        public bool? Status
        {
            get => _Status;
            set
            {
                _Status = value;
                if (value == true)
                    To.Data = From.Data;
            }
        }

        public TaskLink(TaskEndpoint from, TaskEndpoint to)
        {
            From = from;
            To = to;
        }
    }
}
