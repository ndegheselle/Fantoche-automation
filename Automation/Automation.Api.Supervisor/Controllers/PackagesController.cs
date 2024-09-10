using Automation.Shared.Base;
using Microsoft.AspNetCore.Mvc;

namespace Automation.Api.Supervisor.Controllers
{
    [ApiController]
    [Route("packages")]
    public class PackagesController
    {
        protected readonly PackageManagement _packages;
        public PackagesController(PackageManagement packages)
        {
            _packages = packages;
        }

        [HttpGet]
        public Task<IEnumerable<Package>> SearchAsync([FromQuery]string searchValue, [FromQuery] int page, [FromQuery] int pageSize)
        {
            return _packages.SearchAsync(searchValue, page, pageSize);
        }
    }
}
