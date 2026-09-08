namespace Automation.Worker.Packages;

/// <summary>
/// Where the assemblies holding the tasks are resolved from : what an executor needs of a package
/// source is the path of the dll it has to load, whatever it took to get it there.
/// <para>
/// Abstracted from <see cref="LocalPackageManagement"/> so an executor can be run against
/// assemblies already sitting on disk, with no nuget package to pack nor download.
/// </para>
/// </summary>
public interface IPackageManagement
{
    /// <summary>
    /// Make the assemblies of [id] [version] available locally and return the path of every one of
    /// them.
    /// </summary>
    /// <exception cref="PackageDownloadException">The package or its assemblies can't be resolved.</exception>
    Task<IEnumerable<string>> DownloadPackageAsync(string id, Version version);

    /// <summary>
    /// Make the assemblies of [id] [version] available locally and return the path of [dll] (its
    /// name, without the extension) among them.
    /// </summary>
    /// <exception cref="PackageDownloadException">The package or that assembly can't be resolved.</exception>
    Task<string> DownloadPackageAsync(string id, Version version, string dll);
}
