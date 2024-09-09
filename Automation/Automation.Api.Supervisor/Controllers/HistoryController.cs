using Automation.Dal.Models;
using Automation.Dal.Repositories;
using Automation.Shared;
using Automation.Shared.Base;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace Automation.Api.Supervisor.Controllers
{
    [ApiController]
    [Route("histories")]
    public class HistoryController : IHistoryClient<TaskHistory>
    {
        protected readonly HistoryRepository _repository;
        public HistoryController(IMongoDatabase database)
        {
            _repository = new HistoryRepository(database);
        }

        [HttpGet]
        [Route("scopes/{scopeId}")]
        public async Task<ListPageWrapper<TaskHistory>> GetByScopeAsync([FromRoute]Guid scopeId, [FromQuery] int page, [FromQuery] int pageSize)
        {
            return await _repository.GetByScopeAsync(scopeId, page, pageSize);
        }

        [HttpGet]
        [Route("tasks/{taskId}")]
        public async Task<ListPageWrapper<TaskHistory>> GetByTaskAsync([FromRoute] Guid taskId, [FromQuery] int page, [FromQuery] int pageSize)
        {
            return await _repository.GetByTaskAsync(taskId, page, pageSize);
        }
    }
}
