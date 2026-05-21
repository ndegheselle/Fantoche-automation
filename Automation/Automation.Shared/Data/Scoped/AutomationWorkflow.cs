using Automation.Models.Work;

namespace Automation.Shared.Data.Scoped
{
    public class WorkflowSettings
    {
        public bool IsWaitingForAllEnd { get; set; }
    }

    public class AutomationWorkflow : BaseAutomationTask
    {
        /// <summary>
        /// Store all data (input, output, global, ...) in task instance.
        /// </summary>
        public bool IsStoringAllData { get; set; } = false;

        public WorkflowSettings WorkflowSettings { get; set; } = new();

        public Graph Graph { get; set; } = new();

        /// <summary>
        /// Mapping for the output of the workflow.
        /// </summary>
        public string? OutputMappingJson { get; set; }

        /// <summary>
        /// Schema of all the common data of the workflow.
        /// </summary>
        public string? CommonSchemaJson { get; set; }

        public AutomationWorkflow() : base(EnumScopedType.Workflow)
        {
        }
    }
}
