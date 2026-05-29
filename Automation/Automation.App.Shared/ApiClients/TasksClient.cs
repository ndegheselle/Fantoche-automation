using RestSharp;

namespace Automation.App.Shared.ApiClients
{
    // MIGRATION STUB: the original TasksClient (BaseCrudClient<BaseAutomationTask> with
    // GetInstancesAsync / ExecuteAsync / GetTagsAsync / GetByTagAsync) referenced model types
    // from the deleted "Automation.Models" project and the REST API that the app no longer
    // tracks. Stubbed to the DI constructor only. To be reworked against Automation.Worker +
    // SQLite. Original implementation: see git history (pre-'avalonia-migration').
    public class TasksClient : BaseClient
    {
        public TasksClient(RestClient restClient) : base(restClient, "tasks")
        {
        }
    }
}
