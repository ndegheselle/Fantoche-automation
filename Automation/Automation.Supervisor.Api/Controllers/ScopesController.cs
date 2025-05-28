using Automation.Dal.Models;
using Automation.Dal.Repositories;
using Automation.Shared.Base;
using Automation.Shared.Data;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace Automation.Supervisor.Api.Controllers
{
    [ApiController]
    [Route("scopes")]
    public class ScopesController : BaseCrudController<Scope>
    {
        private ScopesRepository _repository => (ScopesRepository)_crudRepository;
        private readonly TaskIntancesRepository _taskInstanceRepo;

        public ScopesController(IMongoDatabase database) : base(new ScopesRepository(database))
        {
            _taskInstanceRepo = new TaskIntancesRepository(database);
        }

        [HttpPost]
        [Route("")]
        public override async Task<ActionResult<Guid>> CreateAsync(Scope element)
        {
            if (element.ParentId == null)
            {
                return BadRequest(new Dictionary<string, string[]>()
                {
                    {nameof(AutomationTask.ParentId), [$"A scope cannot be created without a parent."] }
                });
            }

            var existingChild = await _repository.GetDirectChildByNameAsync(element.ParentId, element.Metadata.Name);
            if (existingChild != null)
            {
                // XXX : if need more info can also use return ValidationProblem(new ValidationProblemDetails());
                return BadRequest(new Dictionary<string, string[]>()
                {
                    {nameof(ScopedMetadata.Name), [$"The name {element.Metadata.Name} is already used in this scope."] }
                });
            }

            var scope = await _repository.GetByIdAsync(element.ParentId.Value);
            if (scope == null)
            {
                return BadRequest(new Dictionary<string, string[]>()
                {
                    {nameof(ScopedMetadata.Name), [$"The parent id {element.ParentId} is invalid."] }
                });
            }

            element.ParentTree = [..scope.ParentTree, scope.Id];
            return await _repository.CreateAsync(element);
        }

        [HttpGet]
        [Route("root")]
        public async Task<Scope> GetRootAsync()
        {
            return await _repository.GetRootAsync();
        }

        [HttpGet]
        [Route("{scopeId}/parents")]
        public async Task<ActionResult<IEnumerable<Scope>>> GetParentScopes([FromRoute] Guid scopeId)
        {
            var scope = await _repository.GetByIdAsync(scopeId);
            if (scope == null)
            {
                return BadRequest(new Dictionary<string, string[]>()
                {
                    {nameof(ScopedMetadata.Name), [$"The scope id {scopeId} is invalid."] }
                });
            }
            return Ok(await _repository.GetByIdsAsync(scope.ParentTree));
        }

        [HttpGet]
        [Route("{id}/instances")]
        public async Task<ListPageWrapper<AutomationTaskInstance>> GetInstancesAsync([FromRoute] Guid id, [FromQuery] int page, [FromQuery] int pageSize)
        {
            return await _taskInstanceRepo.GetByScopeAsync(id, page, pageSize);
        }
    }
}
