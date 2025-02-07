using Automation.Dal.Models;
using Automation.Dal.Repositories;
using Automation.Realtime;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using NuGet.Protocol.Core.Types;

namespace Automation.Supervisor.Api.Controllers
{
    [ApiController]
    [Route("graphs")]
    public class GraphsController : Controller
    {
        private readonly GraphsRepository _repository;
        private readonly IMongoDatabase _database;

        public GraphsController(IMongoDatabase database, RedisConnectionManager redis)
        {
            _database = database;
            _repository = new GraphsRepository(database);
        }

        [HttpPost]
        [Route("")]
        public async Task<ActionResult<Guid>> CreateAsync(Graph element)
        {
            // TODO : validate WorkflowId
            return await _repository.CreateAsync(element);
        }

        [HttpGet]
        [Route("workflows/{workflowId}")]
        public async Task<Graph?> GetByWorkflowIdAsync([FromRoute] Guid workflowId)
        {
            return await _repository.GetByWorkflowId(workflowId);
        }
    }
}
