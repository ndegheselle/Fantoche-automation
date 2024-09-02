using Automation.Dal.Models;
using Automation.Dal.Repositories;
using Automation.Shared.Data;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace Automation.Api.Supervisor.Controllers
{
    [ApiController]
    [Route("tasks")]
    public class TaskController
    {
        protected readonly TaskRepository _repository;
        public TaskController(IMongoDatabase database)
        {
            _repository = new TaskRepository(database);
        }

        [HttpPost]
        [Route("")]
        public Task<Guid> CreateAsync(TaskNode element)
        {
            return _repository.CreateAsync(element);
        }

        [HttpDelete]
        [Route("{id}")]
        public Task DeleteAsync([FromRoute] Guid id)
        {
            return _repository.DeleteAsync(id);
        }

        [HttpGet]
        [Route("{id}")]
        public async Task<ITaskNode?> GetByIdAsync([FromRoute] Guid id)
        {
            return await _repository.GetByIdAsync(id);
        }

        [HttpPut]
        [Route("{id}")]
        public Task UpdateAsync([FromRoute] Guid id, TaskNode element)
        {
            return _repository.UpdateAsync(id, element);
        }
    }
}
