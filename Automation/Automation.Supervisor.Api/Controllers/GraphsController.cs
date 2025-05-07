using Automation.Dal.Models;
using Automation.Dal.Repositories;
using Automation.Realtime;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace Automation.Supervisor.Api.Controllers
{
    [ApiController]
    [Route("graphs")]
    public class GraphsController : BaseCrudController<Graph>
    {
        private GraphsRepository _repository => (GraphsRepository)_crudRepository;

        public GraphsController(IMongoDatabase database, RedisConnectionManager redis) : base(
            new GraphsRepository(database))
        {
        }

        [HttpGet]
        [Route("workflows/{workflowId}")]
        public async Task<Graph?> GetByWorkflowIdAsync([FromRoute] Guid workflowId)
        { return await _repository.GetByWorkflowIdAsync(workflowId); }
    }
}
