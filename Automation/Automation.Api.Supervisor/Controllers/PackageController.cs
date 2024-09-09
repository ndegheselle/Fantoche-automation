using Automation.Shared.Base;
using Microsoft.AspNetCore.Mvc;

namespace Automation.Api.Supervisor.Controllers
{
    [ApiController]
    [Route("packages")]
    public class PackageController
    {
        protected readonly PackageManagement _packages;
        public PackageController(PackageManagement packages)
        {
            _packages = packages;
        }

        [HttpGet]
        public Task<IEnumerable<Package>> GetAll()
        {
            return _packages.GetAll();
        }
    }
}
