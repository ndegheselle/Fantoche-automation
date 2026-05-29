using RestSharp;

namespace Automation.App.Shared.ApiClients
{
    // MIGRATION STUB: the original TaskInstancesClient was BaseCrudClient<TaskInstance>, which
    // referenced a model type from the deleted "Automation.Models" project. Stubbed to the DI
    // constructor only, pending the rework against Automation.Worker + SQLite. See git history.
    public class TaskInstancesClient : BaseClient
    {
        public TaskInstancesClient(RestClient restClient) : base(restClient, "instances")
        {
        }
    }
}
