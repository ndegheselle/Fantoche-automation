using Automation.Dal.Models;
using Automation.Dal.Repositories;
using Automation.Realtime;
using Automation.Supervisor;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace Automation.Api.Supervisor.Controllers
{
    [ApiController]
    [Route("tasks")]
    public class TasksController : Controller
    {
        private readonly TaskRepository _taskRepo;
        private readonly IMongoDatabase _database;
        private readonly TaskSuperviser _supervisor;

        public TasksController(IMongoDatabase database, RedisConnectionManager redis)
        {
            _database = database;
            _taskRepo = new TaskRepository(database);
            _supervisor = new TaskSuperviser(database, redis);
        }

        [HttpPost]
        [Route("")]
        public async Task<ActionResult<Guid>> CreateAsync(TaskNode element)
        {
            var scopeRepository = new ScopeRepository(_database);
            var existingChild = await scopeRepository.GetChildByNameAsync(element.ScopeId, element.Name);

            if (existingChild != null)
            {
                return BadRequest(new Dictionary<string, string[]>()
                {
                    {nameof(TaskNode.Name), [$"The name {element.Name} is already used in this scope."] }
                });
            }

            return await _taskRepo.CreateAsync(element);
        }

        [HttpDelete]
        [Route("{id}")]
        public Task DeleteAsync([FromRoute] Guid id)
        {
            return _taskRepo.DeleteAsync(id);
        }

        [HttpGet]
        [Route("{id}")]
        public async Task<TaskNode?> GetByIdAsync([FromRoute] Guid id)
        {
            return await _taskRepo.GetByIdAsync(id);
        }

        [HttpPut]
        [Route("{id}")]
        public Task UpdateAsync([FromRoute] Guid id, [FromBody]TaskNode element)
        {
            return _taskRepo.UpdateAsync(id, element);
        }

        [HttpPost]
        [Route("{id}/execute")]
        public Task<TaskInstance> Execute([FromRoute] Guid id, [FromBody] object parameters)
        {
            return _supervisor.AssignToWorkerAsync(id, parameters);
        }
    }
}
