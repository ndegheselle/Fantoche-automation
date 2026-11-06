using NuGet.Packaging;
using NuGet.Versioning;

namespace Fantoche.Worker.Tests.Packages;

/// <summary>
/// A real nuget package holding the plugins built next to the tests, for the tests of where the
/// packages come from (the execution tests skip packages entirely, see TestPackageManagement).
/// </summary>
internal static class TestNupkg
{
    public const string Dll = PluginTasks.Package;

    /// <summary>
    /// Write the package [id] [version] in [folder] and return its path, its assembly built for
    /// [framework] : "net10.0-windows7.0" for a package of interfaces.
    /// </summary>
    public static string Write(string folder, string id, string version, string framework = "net10.0")
    {
        var builder = new PackageBuilder
        {
            Id = id,
            Version = NuGetVersion.Parse(version),
            Description = "Built by the tests.",
        };
        builder.Authors.Add("tests");
        builder.Files.Add(new PhysicalPackageFile
        {
            SourcePath = Path.Combine(AppContext.BaseDirectory, $"{Dll}.dll"),
            TargetPath = $"lib/{framework}/{Dll}.dll",
        });

        Directory.CreateDirectory(folder);
        string path = Path.Combine(folder, $"{id}.{version}.nupkg");
        using FileStream stream = File.Create(path);
        builder.Save(stream);
        return path;
    }
}
