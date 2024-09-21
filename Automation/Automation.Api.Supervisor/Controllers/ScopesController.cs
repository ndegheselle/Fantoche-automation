using Automation.Dal.Models;
using Automation.Dal.Repositories;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace Automation.Api.Supervisor.Controllers
{
    [ApiController]
    [Route("scopes")]
    public class ScopesController : Controller
    {
        protected readonly ScopeRepository _repository;
        public ScopesController(IMongoDatabase database)
        {
            _repository = new ScopeRepository(database);
        }

        [HttpPost]
        [Route("")]
        public async Task<ActionResult<Guid>> CreateAsync(Scope element)
        {
            var existingChild = await _repository.GetChildByNameAsync(element.ParentId, element.Name);

            if (existingChild != null)
            {
                // XXX : if need more info can also use return ValidationProblem(new ValidationProblemDetails());
                return BadRequest(new Dictionary<string, string[]>()
                {
                    {nameof(Scope.Name), [$"The name {element.Name} is already used in this scope."] }
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
        public async Task<Scope?> GetByIdAsync([FromRoute] Guid id)
        {
            return await _repository.GetByIdAsync(id);
        }

        [HttpPut]
        [Route("{id}")]
        public Task UpdateAsync([FromRoute] Guid id, Scope element)
        {
            return _repository.UpdateAsync(id, element);
        }

        [HttpGet]
        [Route("root")]
        public async Task<Scope> GetRootAsync()
        {
            return await _repository.GetRootAsync();
        }
    }
}
