using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Versioning;

namespace Fantoche.Worker.Packages;

#region Exceptions

public class PackageDownloadException : Exception
{
    public PackageDownloadException(string message) : base(message) { }
    public PackageDownloadException(string message, Exception ex) : base(message, ex) { }
}

#endregion

/// <summary>
/// The assemblies of the packages, extracted on this machine the first time a task needs them :
/// <c>{folder}/{id}/{version}/*.dll</c>. A package is fetched from its <see cref="IPackageSource"/>
/// once, its version never changing (see <see cref="PackageFolder.AddAsync"/>).
/// </summary>
public class PackageCache : IPackageManagement
{
    private readonly IPackageSource _source;
    private readonly string _folder;
    private readonly NuGetFramework _framework;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _extractionLocks = new();

    /// <summary>
    /// Whether the symbols are extracted along with the assemblies, see <see cref="IsHostDebuggable"/>.
    /// </summary>
    private readonly bool _loadSymbols;

    /// <param name="folderPath">Where the packages are extracted to.</param>
    /// <param name="loadSymbols">
    /// Whether the symbol packages are extracted along with the assemblies they belong to, so the
    /// tasks can be stepped into. Defaults to <see cref="IsHostDebuggable"/> : a released
    /// application has nothing to debug, and the symbols only make the loading heavier.
    /// </param>
    public PackageCache(IPackageSource source, string folderPath, bool? loadSymbols = null)
    {
        _source = source;
        _folder = folderPath;
        _loadSymbols = loadSymbols ?? IsHostDebuggable();
        _framework = GetCurrentFramework();
    }

    #region Download

    /// <exception cref="PackageDownloadException">
    /// The package is not found, has no assembly for this framework, or can't be fetched.
    /// </exception>
    public async Task<IEnumerable<string>> DownloadPackageAsync(string id, Version version)
    {
        string folder = GetPackageFolderPath(id, version);
        if (Directory.Exists(folder))
            return GetDllPaths(folder);

        // Two executions of the same new package would write the same files at once.
        SemaphoreSlim extractionLock = _extractionLocks.GetOrAdd(folder, _ => new SemaphoreSlim(1, 1));
        await extractionLock.WaitAsync();
        try
        {
            // Extracted by the execution this one waited for.
            if (Directory.Exists(folder))
                return GetDllPaths(folder);

            await ExtractAsync(id, version, folder);
            return GetDllPaths(folder);
        }
        catch (PackageDownloadException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new PackageDownloadException($"Failed to download package [id:{id}][version:{version}]: {ex.Message}", ex);
        }
        finally
        {
            extractionLock.Release();
        }
    }

    /// <exception cref="PackageDownloadException">
    /// The package can't be downloaded (see above) or has no assembly named [dll].
    /// </exception>
    public async Task<string> DownloadPackageAsync(string id, Version version, string dll)
    {
        var files = await DownloadPackageAsync(id, version);
        var target = files.FirstOrDefault(x => Path.GetFileName(x) == $"{dll}.dll");
        return target ?? throw new PackageDownloadException($"Could not find dll for [id:{id}][version:{version}] in downloaded package.");
    }

    /// <summary>
    /// The package is extracted aside then moved in place : its folder existing is what tells it
    /// is cached, a failure halfway must not leave one.
    /// </summary>
    private async Task ExtractAsync(string id, Version version, string folder)
    {
        await using Stream package = await _source.OpenPackageAsync(id, version)
            ?? throw new PackageDownloadException($"Package not found [id:{id}][version:{new NuGetVersion(version)}]");

        using var packageReader = new PackageArchiveReader(package);
        var nearestFramework = GetNearestFramework(packageReader)
            ?? throw new PackageDownloadException("No compatible framework version found in the package.");
        var lib = packageReader.GetLibItems().First(x => x.TargetFramework == nearestFramework);

        string extractionFolder = $"{folder}.{Guid.NewGuid():N}.tmp";
        Directory.CreateDirectory(extractionFolder);
        try
        {
            await CopyFilesAsync(packageReader, lib.Items, RootOf(lib), extractionFolder);
            await CopySymbolsAsync(id, version, extractionFolder);
            Directory.Move(extractionFolder, folder);
        }
        catch (IOException) when (Directory.Exists(folder))
        {
            // Another copy of the application extracted it at the same time : the lock above only
            // holds within this process. Its extraction is as good as this one.
        }
        finally
        {
            if (Directory.Exists(extractionFolder))
                Directory.Delete(extractionFolder, true);
        }
    }

    #endregion

    #region Symbols

    /// <summary>
    /// Extract the symbols of [id] [version] when its assemblies are already extracted : added after
    /// them, they would otherwise only be read once the cache is cleared.
    /// </summary>
    public Task ExtractSymbolsAsync(string id, Version version)
    {
        string folder = GetPackageFolderPath(id, version);
        if (!Directory.Exists(folder))
            return Task.CompletedTask;

        return CopySymbolsAsync(id, version, folder);
    }

    /// <summary>
    /// Extracts the debugging symbols of [id] [version] next to its assemblies, so the debugger
    /// finds them on its own : the loader loads the tasks from that very folder.
    /// <para>
    /// A no-op when the symbols aren't wanted (see <see cref="IsHostDebuggable"/>) or when no symbol
    /// package was added for that version. Whatever goes wrong while reading them is swallowed :
    /// missing symbols only cost the ability to step into a task, they must never keep it from running.
    /// </para>
    /// </summary>
    private async Task CopySymbolsAsync(string id, Version version, string folder)
    {
        if (!_loadSymbols)
            return;

        try
        {
            await using Stream? symbols = await _source.OpenSymbolsAsync(id, version);
            if (symbols == null)
                return;

            using var packageReader = new PackageArchiveReader(symbols);
            var nearestFramework = GetNearestFramework(packageReader);
            var lib = packageReader.GetLibItems().FirstOrDefault(x => x.TargetFramework == nearestFramework);
            if (lib == null)
                return;

            // Only what is missing is written : the pdb of a loaded assembly is locked by the
            // debugger, and rewriting it identically would fail for nothing.
            string root = RootOf(lib);
            List<string> items = [.. lib.Items.Where(x => !File.Exists(PathIn(folder, x, root)))];
            await CopyFilesAsync(packageReader, items, root, folder);
        }
        catch (Exception)
        {
            // Debugging is a convenience, a broken symbol package doesn't break the execution.
        }
    }

    /// <summary>
    /// Whether the running application is itself a debug build, in which case the tasks it loads
    /// are worth being debuggable too. A release application has nothing to step into, so it skips
    /// the symbols entirely.
    /// </summary>
    public static bool IsHostDebuggable()
    {
        var debuggable = Assembly.GetEntryAssembly()?.GetCustomAttribute<DebuggableAttribute>();
        if (debuggable == null)
            return Debugger.IsAttached;

        return debuggable.IsJITOptimizerDisabled;
    }

    #endregion

    #region Files

    /// <summary>
    /// The files are written in [folder] where they stand under [root] in the package : the
    /// assemblies next to each other, where the loader resolves the dependencies of a task, and the
    /// page of a user interface task in its own folder (see <c>IUserInterfaceTask.PageFolder</c>).
    /// A file out of [root] is written at the top.
    /// </summary>
    private static async Task CopyFilesAsync(PackageArchiveReader packageReader, IEnumerable<string> items, string root, string folder)
    {
        foreach (string item in items)
        {
            string path = PathIn(folder, item, root);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await using Stream source = packageReader.GetStream(item);
            await using FileStream target = File.Create(path);
            await source.CopyToAsync(target);
        }
    }

    /// <summary>Where the files of the framework [lib] stand in the package.</summary>
    private static string RootOf(FrameworkSpecificGroup lib) => $"lib/{lib.TargetFramework.GetShortFolderName()}/";

    /// <summary>Where [item] of the package, standing under [root], is written in [folder].</summary>
    /// <exception cref="PackageDownloadException">[item] points out of [folder].</exception>
    private static string PathIn(string folder, string item, string root)
    {
        string relative = item.StartsWith(root, StringComparison.OrdinalIgnoreCase) ? item[root.Length..] : Path.GetFileName(item);
        string path = Path.GetFullPath(Path.Combine(folder, relative));
        // A path of the package is only trusted to stay within the folder of the package.
        if (!path.StartsWith(Path.GetFullPath(folder) + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            throw new PackageDownloadException($"The file [{item}] of the package points out of it.");
        return path;
    }

    private static IEnumerable<string> GetDllPaths(string folder) => Directory.EnumerateFiles(folder, "*.dll");

    private string GetPackageFolderPath(string id, Version version)
    {
        var nugetVersion = new NuGetVersion(version);
        return Path.Combine(_folder, id, nugetVersion.ToNormalizedString());
    }

    #endregion

    #region Framework Resolution

    /// <summary>
    /// The framework of the package nearest to the one running, null when none is compatible.
    /// </summary>
    private NuGetFramework? GetNearestFramework(PackageArchiveReader packageReader)
    {
        var frameworks = packageReader.GetLibItems()
            .Select(group => group.TargetFramework)
            .Where(f => f != null)
            .ToList();

        return NuGetFrameworkUtility.GetNearest(frameworks, _framework, f => f);
    }

    /// <summary>
    /// The runtime rather than the framework the entry assembly targets : what matters is what the
    /// running process can load, and the entry assembly isn't ours under a test host.
    /// </summary>
    private static NuGetFramework GetCurrentFramework()
        => new(FrameworkConstants.FrameworkIdentifiers.NetCoreApp, new Version(Environment.Version.Major, Environment.Version.Minor));

    #endregion
}
