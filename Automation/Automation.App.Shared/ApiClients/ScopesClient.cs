using RestSharp;

namespace Automation.App.Shared.ApiClients
{
    // MIGRATION STUB: the original ScopesClient (BaseCrudClient<Scope> with GetRootAsync /
    // GetParentScopes / GetInstancesAsync) referenced model types from the deleted
    // "Automation.Models" project. Stubbed to the DI constructor only, pending the planned
    // rework against Automation.Worker + SQLite. Original: see git history.
    public class ScopesClient : BaseClient
    {
        public ScopesClient(RestClient restClient) : base(restClient, "scopes")
        {
        }
    }
}
