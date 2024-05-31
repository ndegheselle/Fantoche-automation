using Automation.Base;
using System.Text.Json;

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

        public Task<bool> ExecuteTask()
        {
            ITask? task = Activator.CreateInstance(TaskType) as ITask;
            if (task == null)
                throw new Exception($"'{TaskType}' is not an ITask");

            task.Context = Context;
            return task.Start();
        }

        private dynamic? LoadContext(string serializedContext)
        {
            if (string.IsNullOrWhiteSpace(serializedContext))
                return null;

            // deserialize the context from json
            return JsonSerializer.Deserialize<dynamic>(serializedContext);
        }
    }
}
