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

        private ITask _EndingTask
        {
            get
            {
                IEnumerable<ITask> endingTasks = Tasks.Where(t => t is GraphFlowEnd);
                if (endingTasks.Count() == 0)
                    throw new Exception("No ending task found");
                if (endingTasks.Count() > 1)
                    throw new Exception("Multiple ending tasks found");
                return endingTasks.First();
            }
        }

        public override async Task<EnumTaskStatus> Start()
        {
            if (!ValidateInputs(inputs))
                return EnumTaskStatus.Failed;

            await StartNode(_StartingTask);

            return EnumTaskStatus.Completed;
        }

        private async Task<EnumTaskStatus> StartNode(ITask node)
        {
            EnumTaskStatus status = await node.Start();
            if (status == EnumTaskStatus.Failed)
                return EnumTaskStatus.Failed;

            foreach (var link in Links.Where(l => l.Source == node))
            {
                link.Status = EnumTaskStatus.Completed;
                var linkInputs = Links.Where(l => l.Target == link.Target);
                // If all inputs are not completed, skip
                if (linkInputs.Any(x => x.Status != EnumTaskStatus.Completed))
                    break;

                EnumTaskStatus statut = await StartNode(link.Target);

                if (statut == EnumTaskStatus.Failed)
                    return EnumTaskStatus.Failed;
            }
            return EnumTaskStatus.Completed;
        }
    }

    public class TaskLink
    {
        public string SourceOutput { get; set; }
        public ITask Source { get; set; }
        public string TargetInput { get; set; }
        public ITask Target { get; set; }

        private EnumTaskStatus _status;
        public EnumTaskStatus Status
        {
            get => _status;
            set
            {
                _status = value;
                if (value == EnumTaskStatus.Completed)
                    OnCompleted();
            }
        }

        public void OnCompleted()
        {
            // Set the target input data
            Target.Inputs[TargetInput].Data = Source.Outputs[SourceOutput].Data;
        }
    }
}
