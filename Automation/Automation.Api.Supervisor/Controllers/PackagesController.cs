using Automation.Shared.Base;
using Automation.Shared.Packages;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;

namespace Automation.Api.Supervisor.Controllers
{
    [ApiController]
    [Route("packages")]
    public class PackagesController : Controller
    {
        protected readonly IPackageManagement _packages;
        public PackagesController(IPackageManagement packages) { _packages = packages; }

        [HttpGet]
        public Task<ListPageWrapper<PackageInfos>> SearchAsync(
            [FromQuery]string? searchValue,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        { return _packages.SearchAsync(searchValue ?? "", page, pageSize); }

        [HttpPost]
        public async Task<ActionResult<PackageInfos>> CreatePackage(IFormFile file)
        {
            string extension = Path.GetExtension(file.FileName);
            string tempFilePath = Path.GetTempFileName();
            try
            {
                using (var fileStream = new FileStream(tempFilePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }

                if (!_packages.IsFileValidPackage(tempFilePath, out List<string> errors))
                {
                    return BadRequest(new Dictionary<string, string[]>() { { nameof(file), errors.ToArray() } });
                }
                return await _packages.CreatePackageFromFileAsync(tempFilePath);
            } finally
            {
                // Ensure the temporary file is deleted even if an exception occurs
                if (System.IO.File.Exists(tempFilePath))
                {
                    System.IO.File.Delete(tempFilePath);
                }
            }
        }
    }
}
