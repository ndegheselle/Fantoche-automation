using Automation.Models.Work;
using Automation.Worker.Control;
using Newtonsoft.Json.Linq;

namespace Automation.Worker.Executor
{
    public class TaskChangeToConsole : ITaskChangeHandler
    {
        public void OnTaskEnd(BaseAutomationTask automationTask, TaskOutput output, WorkflowContext? workflowContext)
        {
            Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss.fff")} - {automationTask.Metadata.Name} - {output.State} - {output.OutputToken?.ToString()}");
        }

        public void OnTaskStart(BaseAutomationTask automationTask, JToken? input, WorkflowContext? workflowContext)
        {
            Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss.fff")} - {automationTask.Metadata.Name} - Start");
        }
    }
}
