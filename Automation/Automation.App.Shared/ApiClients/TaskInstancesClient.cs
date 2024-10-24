using Automation.App.Shared.ViewModels.Tasks;
using Automation.Shared.Base;
using Automation.Shared.Clients;
using RestSharp;

namespace Automation.App.Shared.ApiClients
{
    public class TaskInstancesClient : BaseClient
    {
        public TaskInstancesClient(RestClient restClient) : base(restClient, "histories") 
        { }



    }
}
