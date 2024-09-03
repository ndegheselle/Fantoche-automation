using Automation.Dal.Models;
using Automation.Dal.Repositories;
using Automation.Shared.Data;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace Automation.Api.Supervisor.Controllers
{
    [ApiController]
    [Route("scopes")]
    public class ScopeController
    {
        protected readonly ScopeRepository _repository;
        public ScopeController(IMongoDatabase database)
        {
            _repository = new ScopeRepository(database);
        }

        [HttpPost]
        [Route("")]
        public Task<Guid> CreateAsync(Scope element)
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
