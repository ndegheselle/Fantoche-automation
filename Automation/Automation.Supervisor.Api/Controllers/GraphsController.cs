using Automation.Dal.Models;
using Automation.Dal.Repositories;
using Automation.Realtime;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace Automation.Supervisor.Api.Controllers
{
    [ApiController]
    [Route("graphs")]
    public class GraphsController : Controller
    {
        private readonly GraphsRepository _graphRepo;
        private readonly IMongoDatabase _database;

        public GraphsController(IMongoDatabase database, RedisConnectionManager redis)
        {
            _database = database;
            _graphRepo = new GraphsRepository(database);
        }

        [HttpGet]
        [Route("{id}")]
        public async Task<Graph?> GetByIdAsync([FromRoute] Guid workflowId)
        {
            return await _graphRepo.GetByWorkflowId(workflowId);
        }
    }
}
