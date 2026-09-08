using Automation.Worker.Packages;

namespace Automation.Worker.Tests;

/// <summary>
/// The packages of a test : the assemblies are the ones already built next to the tests, so a
/// workflow can be run against the real plugins without packing nor downloading anything.
/// <para>
/// A package identifier stands for the assembly of the same name, whatever version is asked for :
/// what a run needs of a package source is the path of the dll to load (see
/// <see cref="IPackageManagement"/>), and the tests only ever load the one they were built with.
/// </para>
/// </summary>
internal sealed class TestPackageManagement : IPackageManagement
{
    private readonly string _folder;

    /// <summary>
    /// Every resolution asked for, in order, as "id/version/dll" (the dll being empty when every
    /// assembly of the package was asked for).
    /// </summary>
    public List<string> Requests { get; } = [];

    public TestPackageManagement(string? folder = null)
    {
        _folder = folder ?? AppContext.BaseDirectory;
    }

    public Task<IEnumerable<string>> DownloadPackageAsync(string id, Version version)
    {
        lock (Requests)
            Requests.Add($"{id}/{version}/");

        return Task.FromResult<IEnumerable<string>>([Resolve(id, version, id)]);
    }

    public Task<string> DownloadPackageAsync(string id, Version version, string dll)
    {
        lock (Requests)
            Requests.Add($"{id}/{version}/{dll}");

        return Task.FromResult(Resolve(id, version, dll));
    }

    private string Resolve(string id, Version version, string dll)
    {
        string path = Path.Combine(_folder, $"{dll}.dll");
        if (!File.Exists(path))
            throw new PackageDownloadException($"Package not found [id:{id}][version:{version}][dll:{dll}]");
        return path;
    }
}
