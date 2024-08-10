using Automation.Dal.Models;
using Automation.Dal.Repositories;
using Automation.Shared;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace Automation.Api.Supervisor.Controllers
{
    [ApiController]
    [Route("scopes")]
    public class ScopeController : BaseCrudController<ScopeRepository, Scope>, IScopeRepository<Scope>
    {
        public ScopeController(IMongoDatabase database) : base(new ScopeRepository(database))
        {}

        [HttpGet]
        [Route("root")]
        public Task<Scope> GetRootAsync()
        {
            return _repository.GetRootAsync();
        }
    }
}
