using Automation.Models.Work;
using Automation.Worker.Control;
using Newtonsoft.Json.Linq;

namespace Automation.Worker.Executor
{
    public class TaskChangeToConsole : ITaskChangeHandler
    {
        public void OnTaskEnd(BaseGraphTask graphTask, TaskOutput output, WorkflowContext? workflowContext)
        {
            Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss.fff")} - {graphTask.Metadata.Name} - {output.State} - {output.OutputToken?.ToString()}");
        }

        public void OnTaskStart(BaseGraphTask graphTask, JToken? input, WorkflowContext? workflowContext)
        {
            Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss.fff")} - {graphTask.Metadata.Name} - Start");
        }
    }
}
