using Automation.Shared.Base;
using Automation.Shared.Data.Execution;

namespace Automation.Shared.Services;

public class PackageValidationException : Exception
{
    public PackageValidationException(string message) : base(message) { }
    public PackageValidationException(string message, Exception inner) : base(message, inner) { }
}

public class PackageAdded
{
    public PackageInfos Infos { get; set; }
    public List<Warning> Warnings { get; set; }
}

public interface IPackagesService
{
    public Task<Paginated<PackageInfos>> SearchAsync(string search = "", PaginationOptions options = default);
    
    /// <summary>
    /// Add a new package
    /// </summary>
    /// <param name="filePath"></param>
    /// <returns>Warnings</returns>
    /// <exception cref="PackageValidationException">If the package is no valid</exception>
    public Task<PackageAdded> AddAsync(string filePath);
    
    public Task RemoveAsync(string id, Version version);
}