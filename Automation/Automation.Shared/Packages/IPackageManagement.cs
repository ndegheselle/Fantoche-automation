using Automation.Plugins.Shared;
using Automation.Shared.Base;
using Automation.Shared.Data;

namespace Automation.Shared.Packages
{
    public interface IPackageManagement
    {
        /// <summary>
        /// Search for a package
        /// </summary>
        /// <param name="search">package id to search</param>
        /// <param name="page">page number, start at 1</param>
        /// <param name="pageSize">number of items on the page</param>
        /// <returns>List of found packages</returns>
        Task<ListPageWrapper<PackageInfos>> SearchAsync(string search = "", int page = 1, int pageSize = 50);

        /// <summary>
        /// Get the infos of the package with the given id (if exist)
        /// </summary>
        /// <param name="packageId">target id</param>
        /// <param name="version">optional specific version, if null returns latest</param>
        /// <returns>Infos of the package if exist</returns>
        Task<PackageInfos> GetInfosAsync(string packageId, Version? version);

        /// <summary>
        /// Get the infos of the package from a file stream
        /// </summary>
        /// <param name="stream">Stream containing the package data</param>
        /// <returns>Infos of the package</returns>
        PackageInfos GetInfosFromStream(Stream stream);

        Task<IEnumerable<Version>> GetVersionsAsync(string packageId);

        /// <summary>
        /// Create a package from a valid package file stream
        /// </summary>
        /// <param name="stream">Stream containing the package data</param>
        /// <returns>Infos of the created package</returns>
        Task<PackageInfos> CreateFromStreamAsync(Stream stream);

        /// <summary>
        /// Remove a package version
        /// </summary>
        /// <param name="id">Target package id</param>
        /// <param name="version">Target package version</param>
        Task RemoveAsync(string id, Version version);

        /// <summary>
        /// Get all assemblies contained in a specific package version
        /// </summary>
        /// <param name="id">Target package id</param>
        /// <param name="version">Target package version</param>
        /// <returns>Collection of assembly file paths within the package</returns>
        Task<IEnumerable<PackageClass>> GetTaskClassesAsync(string id, Version version);

        /// <summary>
        /// Create a task package instance from a class name
        /// </summary>
        /// <param name="package">Target package informations</param>
        /// <returns>Instance of the class</returns>
        Task<ITask> CreateTaskInstanceAsync(TargetedPackageClass package);
    }
}
