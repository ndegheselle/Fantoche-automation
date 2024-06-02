using System.Xml.Linq;
using Automation.Base.Tasks;

namespace Automation.Base
{
    public class TaskWorkflow : TaskBase
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

            bool result = await ExecuteNode(startingTask);

            // Set outputs if the workflow is designed that way
            ITask endingTask = GetEndingTask();
            if (endingTask != null)
                this.Outputs = endingTask.Outputs;

            return result;
        }
        
        private ITask GetStartingTask()
        {
            IEnumerable<ITask> startingTasks = Tasks.Where(t => t is WorkflowStart);
            if (startingTasks.Count() == 0)
                throw new Exception("No starting task found");
            if (startingTasks.Count() > 1)
                throw new Exception("Multiple starting tasks found");

            return startingTasks.First();
        }

        private ITask? GetEndingTask()
        {
            IEnumerable<ITask> endingTasks = Tasks.Where(t => t is WorkflowEnd);
            if (endingTasks.Count() > 1)
                throw new Exception("Multiple ending tasks found");

            return endingTasks.FirstOrDefault();
        }

        private async Task<bool> ExecuteNode(ITask node)
        {
            bool result = await node.Start();

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
