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
        public List<ITask> Tasks { get; set; }
        public List<TaskLink> Links { get; set; }

        private ITask _StartingTask
        {
            get
            {
                IEnumerable<ITask> startingTasks = Tasks.Where(t => t is GraphFlowStart);
                if (startingTasks.Count() == 0)
                    throw new Exception("No starting task found");
                if (startingTasks.Count() > 1)
                    throw new Exception("Multiple starting tasks found");

                return startingTasks.First();
            }
        }

        private ITask _CurentTask { get; set; }

        public override async Task<EnumTaskStatus> Start(Dictionary<string, dynamic> inputs)
        {
            if (!ValidateInputs(inputs))
                return EnumTaskStatus.Failed;

            _CurentTask = _StartingTask;

            while (!(_CurentTask is GraphFlowEnd))
            {
                EnumTaskStatus status = await _CurentTask.Start(inputs);
                if (status == EnumTaskStatus.Failed)
                    return EnumTaskStatus.Failed;


            }

            Outputs = _CurentTask.Outputs;
            return EnumTaskStatus.Completed;
        }

        
    }

    public class TaskLink
    {
        public string SourceOutput { get; set; }
        public ITask Source { get; set; }
        public string TargetInput { get; set; }
        public ITask Target { get; set; }
    }
}
