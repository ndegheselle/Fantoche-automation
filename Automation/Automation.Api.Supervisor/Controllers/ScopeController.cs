using Automation.Dal.Models;
using Automation.Dal.Repositories;
using Automation.Shared;
using Automation.Shared.Data;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace Automation.Api.Supervisor.Controllers
{
    [ApiController]
    [Route("scopes")]
    public class ScopeController : IScopeClient<IScope>
    {
        protected readonly ScopeRepository _repository;
        public ScopeController(IMongoDatabase database)
        {
            _repository = new ScopeRepository(database);
        }

        [HttpPost]
        [Route("")]
        public Task<Guid> CreateAsync(IScope element)
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
        public async Task<IScope?> GetByIdAsync([FromRoute] Guid id)
        {
            return await _repository.GetByIdAsync(id);
        }

        [HttpGet]
        [Route("root")]
        public async Task<IScope> GetRootAsync()
        {
            return await _repository.GetRootAsync();
        }

        [HttpPut]
        [Route("{id}")]
        public Task UpdateAsync([FromRoute] Guid id, IScope element)
        {
            return _repository.UpdateAsync(id, element);
        }
    }
}
