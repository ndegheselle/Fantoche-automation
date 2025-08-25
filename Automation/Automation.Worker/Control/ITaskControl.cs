using Automation.Dal.Models;

namespace Automation.Worker.Control
{
    public class WorkflowContext
    {
        // Graph
        // Current token and so on
    }

    public interface ITaskControl
    {
        public Task<EnumTaskState> DoAsync(WorkflowContext context);
    }
}
