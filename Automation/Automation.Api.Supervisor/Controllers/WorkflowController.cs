using Automation.Dal.Models;
using Automation.Dal.Repositories;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace Automation.Api.Supervisor.Controllers
{
    [ApiController]
    [Route("workflows")]
    public class WorkflowController
    {
        protected readonly WorkflowRepository _repository;
        public WorkflowController(IMongoDatabase database)
        {
            _repository = new WorkflowRepository(database);
        }

        [HttpPost]
        [Route("")]
        public Task<Guid> CreateAsync(WorkflowNode element)
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
        public async Task<WorkflowNode?> GetByIdAsync([FromRoute] Guid id)
        {
            return await _repository.GetByIdAsync(id);
        }

        [HttpPut]
        [Route("{id}")]
        public Task UpdateAsync([FromRoute] Guid id, WorkflowNode element)
        {
            return _repository.UpdateAsync(id, element);
        }
    }
}
