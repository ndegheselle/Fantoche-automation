using Automation.Dal.Models;
using Automation.Dal.Repositories;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace Automation.Api.Supervisor.Controllers
{
    [ApiController]
    [Route("tasks")]
    public class TasksController : Controller
    {
        protected readonly TaskRepository _repository;
        protected readonly IMongoDatabase _database;

        public TasksController(IMongoDatabase database)
        {
            _database = database;
            _repository = new TaskRepository(database);
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

            return await _repository.CreateAsync(element);
        }

        [HttpDelete]
        [Route("{id}")]
        public Task DeleteAsync([FromRoute] Guid id)
        {
            return _repository.DeleteAsync(id);
        }

        [HttpGet]
        [Route("{id}")]
        public async Task<TaskNode?> GetByIdAsync([FromRoute] Guid id)
        {
            return await _repository.GetByIdAsync(id);
        }

        [HttpPut]
        [Route("{id}")]
        public Task UpdateAsync([FromRoute] Guid id, [FromBody]TaskNode element)
        {
            return _repository.UpdateAsync(id, element);
        }
    }
}
