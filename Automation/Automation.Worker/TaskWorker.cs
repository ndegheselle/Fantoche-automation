using Automation.Base;
using Automation.Plugins.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Automation.Worker
{
    public class TaskWorker
    {
        private readonly Type TaskType;
        private readonly dynamic? Context;

        public TaskWorker(Type taskType, string serializedContext)
        {
            TaskType = taskType;
            Context = LoadContext(serializedContext);
        }

        public Task<EnumTaskStatus> ExecuteTask(Dictionary<string, object> inputs)
        {
            ITask? task = Activator.CreateInstance(TaskType) as ITask;
            if (task == null)
                throw new Exception("Task is not an ITask");

            task.Context = Context;
            return task.Start(inputs);
        }

        private dynamic? LoadContext(string serializedContext)
        {
            // deserialize the context from json
            return JsonSerializer.Deserialize<dynamic>(serializedContext);
        }
    }
}
