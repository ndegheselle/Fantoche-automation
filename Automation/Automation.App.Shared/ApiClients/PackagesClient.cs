using RestSharp;

namespace Automation.App.Shared.ApiClients
{
    // MIGRATION STUB: the original PackagesClient (SearchAsync / GetByIdAsync / GetVersionsAync /
    // GetClassesAsync / CreateAsync / CreatePackageVersionAsync / RemoveFromVersionAsync)
    // referenced types that have drifted/been removed (e.g. ClassIdentifier no longer exists;
    // package types moved to Automation.Shared.Data.Execution). Stubbed to the DI constructor
    // only, pending the rework against Automation.Worker + SQLite. Original: see git history.
    public class PackagesClient : BaseClient
    {
        public PackagesClient(RestClient restClient) : base(restClient, "packages")
        {
        }
    }
}
