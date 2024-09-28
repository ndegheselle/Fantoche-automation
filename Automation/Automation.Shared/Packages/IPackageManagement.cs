using Automation.Shared.Base;

namespace Automation.Shared.Packages
{
    public interface IPackageManagement
    {
        /// <summary>
        /// Search for a package
        /// </summary>
        /// <param name="id">package id to search</param>
        /// <param name="page">page number, start at 1</param>
        /// <param name="pageSize">number of items on the page</param>
        /// <returns>List of found packages</returns>
        Task<ListPageWrapper<PackageInfos>> SearchAsync(string id = "", int page = 1, int pageSize = 50);

        /// <summary>
        /// Get the infos of the package with the given id (if exist)
        /// </summary>
        /// <param name="packageId">target id</param>
        /// <returns>Infos of the package if exist</returns>
        Task<PackageInfos?> GetInfosFromIdAsync(string packageId);

        /// <summary>
        /// Get the infos of the package from a file 
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns>Infos of the package</returns>
        Task<PackageInfos> GetInfosFromFileAsync(string filePath);

        /// <summary>
        /// Check if a file is a valid package
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="errors">list of errors if the file is not a valid package</param>
        /// <returns>True if the file is a valid package</returns>
        bool IsFileValidPackage(string filePath, out List<string> errors);

        /// <summary>
        /// Create a package from a valid package file
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="package">Infos of the package if already retrieved</param>
        /// <returns></returns>
        Task<PackageInfos> CreatePackageFromFileAsync(string filePath, PackageInfos? package = null);

        /// <summary>
        /// Remove a package version
        /// </summary>
        /// <param name="id">Target package id</param>
        /// <param name="version">Target package version</param>
        void RemoveFromIdAndVersion(string id, string version);
    }
}
