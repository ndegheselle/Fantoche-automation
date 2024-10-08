using Automation.Api.Shared;
using Automation.Shared.Base;
using Automation.Shared.Data;
using Microsoft.AspNetCore.Mvc;

namespace Automation.Api.Supervisor.Controllers
{
    [ApiController]
    [Route("packages")]
    public class PackagesController : Controller
    {
        protected readonly IPackageManagement _packages;
        public PackagesController(IPackageManagement packages) { _packages = packages; }

        [HttpGet("{id}")]
        public Task<PackageInfos> GetById([FromRoute] string id)
        { return _packages.GetInfosAsync(id, null); }

        [HttpGet("search")]
        public Task<ListPageWrapper<PackageInfos>> Search(
            [FromQuery]string? searchValue,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        { return _packages.SearchAsync(searchValue ?? "", page, pageSize); }

        [HttpPost]
        public async Task<ActionResult<PackageInfos>> CreatePackage(IFormFile file)
        {
            using var stream = file.OpenReadStream();
            PackageInfos infos;
            try
            {
                infos = _packages.GetInfosFromStream(stream);
            }
            catch (InvalidOperationException)
            {
                return BadRequest(new Dictionary<string, string[]>() { { nameof(file), ["The file is not a valid nuget package."] } });
            }
            return await _packages.CreateFromStreamAsync(stream);
        }

        [HttpGet("{id}/versions")]
        public Task<IEnumerable<Version>> GetVersions([FromRoute] string id)
        { return _packages.GetVersionsAsync(id); }

        [HttpGet("{id}/versions/{version}/classes")]
        public Task<IEnumerable<PackageClass>> GetClasses([FromRoute] string id, [FromRoute] string version)
        { return _packages.GetTaskClassesAsync(id, new Version(version)); }

        [HttpGet("{id}/versions/{version}")]
        public Task<PackageInfos> GetByIdAndVersion([FromRoute] string id, [FromRoute] string version)
        { return _packages.GetInfosAsync(id, new Version(version)); }

        [HttpPost]
        [Route("{id}/versions")]
        public async Task<ActionResult<PackageInfos>> CreatePackageVersion([FromRoute] string id, [FromForm] IFormFile file)
        {
            using var stream = file.OpenReadStream();
            PackageInfos infos;
            try
            {
                infos = _packages.GetInfosFromStream(stream);
            }
            catch (InvalidOperationException)
            {
                return BadRequest(new Dictionary<string, string[]>() { { nameof(file), ["The file is not a valid nuget package."] } });
            }

            if (infos.Id != id)
            {
                return BadRequest(new Dictionary<string, string[]>() { { nameof(id), ["The package id and the nuget package id does not correspond."] } });
            }
            return await _packages.CreateFromStreamAsync(stream);
        }

        [HttpDelete]
        [Route("{id}/versions/{version}")]
        public async Task DeletePackageVersion([FromRoute] string id, [FromRoute] string version)
        {
            await _packages.RemoveAsync(id, new Version(version));
        }
    }
}
