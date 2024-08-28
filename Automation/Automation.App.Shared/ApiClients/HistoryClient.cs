using Automation.App.Shared.ViewModels.Tasks;
using Automation.Shared;
using Automation.Shared.Base;
using RestSharp;

namespace Automation.App.Shared.ApiClients
{
    public class HistoryClient : BaseClient, IHistoryClient<TaskHistory>
    {
        public HistoryClient(RestClient restClient) : base(restClient, "histories") 
        { }

        public async Task<ListPageWrapper<TaskHistory>> GetByScopeAsync(Guid scopeId, int page, int pageSize)
        {
            return await _client.GetAsync<ListPageWrapper<TaskHistory>>(
                new RestRequest($"{_routeBase}/scope/{scopeId}")
                .AddParameter("page", page)
                .AddParameter("pageSize", pageSize)) 
                ?? new ListPageWrapper<TaskHistory>();
        }

        public async Task<ListPageWrapper<TaskHistory>> GetByTaskAsync(Guid taskId, int page, int pageSize)
        {
            return await _client.GetAsync<ListPageWrapper<TaskHistory>>(
                new RestRequest($"{_routeBase}/tasks/{taskId}")
                .AddParameter("page", page)
                .AddParameter("pageSize", pageSize))
                ?? new ListPageWrapper<TaskHistory>();
        }
    }
}
