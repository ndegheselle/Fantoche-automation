using Automation.Dal.Repositories;
using Automation.Shared.Base;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace Automation.Supervisor.Api.Controllers
{
    public class BaseCrudController<T> : Controller where T : IIdentifier
    {
        protected readonly BaseCrudRepository<T> _crudRepository;

        public BaseCrudController(BaseCrudRepository<T> repository)
        {
            _crudRepository = repository;
        }

        [HttpPost]
        [Route("")]
        public virtual async Task<ActionResult<Guid>> CreateAsync(T element)
        {
            return await _crudRepository.CreateAsync(element);
        }

        [HttpGet]
        [Route("")]
        public virtual async Task<List<T>> GetAllASync()
        {
            return await _crudRepository.GetAllAsync();
        }

        [HttpGet]
        [Route("{id}")]
        public virtual async Task<T?> GetByIdAsync([FromRoute] Guid id)
        {
            return await _crudRepository.GetByIdAsync(id);
        }

        [HttpPut]
        [Route("{id}")]
        public virtual async Task UpdateAsync([FromRoute] Guid id, [FromBody] T element)
        {
            await _crudRepository.ReplaceAsync(id, element);
        }

        [HttpDelete]
        [Route("{id}")]
        public virtual Task DeleteAsync([FromRoute] Guid id)
        {
            return _crudRepository.DeleteAsync(id);
        }
    }
}
