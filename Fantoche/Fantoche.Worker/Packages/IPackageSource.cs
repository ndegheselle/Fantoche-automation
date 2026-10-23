namespace Fantoche.Worker.Packages;

/// <summary>
/// Where a <see cref="PackageCache"/> fetches the package files from : the folder holding them
/// (<see cref="PackageFolder"/>) or the supervisor serving its own (<see cref="SupervisorPackageSource"/>).
/// </summary>
public interface IPackageSource
{
    /// <summary>The ".nupkg" of [id] [version], null when the source doesn't hold it.</summary>
    Task<Stream?> OpenPackageAsync(string id, Version version);

    /// <summary>The ".snupkg" of [id] [version], null when none was added.</summary>
    Task<Stream?> OpenSymbolsAsync(string id, Version version);
}
