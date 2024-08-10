using Automation.Shared;
using Automation.Shared.Data;
using Microsoft.AspNetCore.Mvc;
using System.Xml.Linq;

namespace Automation.Api.Supervisor.Controllers
{
    public abstract class BaseCrudController<TRepo, TModel> : ControllerBase where TRepo : ICrudRepository<TModel>
    {
        protected readonly TRepo _repository;
        public BaseCrudController(TRepo crudRepositiory)
        {
            _repository = crudRepositiory;
        }

        [HttpPost]
        [Route("")]
        public Task CreateAsync(TModel element)
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
        public Task<TModel?> GetByIdAsync([FromRoute] Guid id)
        {
            return _repository.GetByIdAsync(id);
        }

        [HttpPut]
        [Route("{id}")]
        public Task UpdateAsync([FromRoute] Guid id, TModel element)
        {
            return _repository.UpdateAsync(id, element);
        }
    }
}
