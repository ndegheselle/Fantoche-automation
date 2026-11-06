using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Fantoche.Plugins.Shared.Ui;
using Fantoche.Shared.Data.Execution;
using Fantoche.Worker.Packages;

namespace Fantoche.App.Features.UserInterfaces
{
    /// <summary>
    /// The interfaces of the tasks (<see cref="ITaskInterface"/>), which only the application loads :
    /// a package of interfaces is extracted for Windows, apart from the cache of the worker.
    /// <para>
    /// Each version of a package is loaded in a context of its own, with the assembly of the tasks
    /// it carries : two versions of an interface are shown side by side. A context is never
    /// unloaded, WPF keeping hold of the types it showed until the application closes.
    /// </para>
    /// </summary>
    public sealed class TaskInterfaces
    {
        private static readonly string ContractAssembly = typeof(ITaskInterface).Assembly.GetName().Name!;

        private readonly PackageCache _packages;

        /// <summary>Keyed by the path of their dll, one per version of a package.</summary>
        private readonly Dictionary<string, PluginLoader<ITaskInterface>> _loaders = new(StringComparer.OrdinalIgnoreCase);

        /// <param name="folder">Where the packages of interfaces are extracted.</param>
        public TaskInterfaces(IPackageSource source, string folder)
        {
            _packages = new PackageCache(source, folder, framework: PackageCache.WindowsFramework);
        }

        /// <summary>A new instance of the interface [target] points at, its package downloaded if needed.</summary>
        /// <exception cref="PackageDownloadException">The package or its assembly can't be resolved.</exception>
        /// <exception cref="InvalidOperationException">The assembly clashes with one already loaded, see <see cref="LoaderOf"/>.</exception>
        public async Task<ITaskInterface> CreateAsync(PackageClassTarget target)
        {
            string dllPath = await _packages.DownloadPackageAsync(target.Package.Id, target.Package.Version, target.Dll);
            PluginLoader<ITaskInterface> loader = LoaderOf(dllPath);
            return loader.CreateInstance(target.ClassFullName);
        }

        /// <summary>The interfaces [id] [version] holds.</summary>
        public async Task<IEnumerable<ClassTarget>> GetClassesAsync(string id, Version version)
        {
            PackageIdentifier identifier = new() { Id = id, Version = version };
            IEnumerable<string> dllPaths = await _packages.DownloadPackageAsync(id, version);

            List<ClassTarget> classes = [];
            // The assembly of the tasks it carries isn't loaded on its own : only the interfaces are,
            // the tasks being loaded along by their context.
            foreach (string dllPath in dllPaths.Where(ReferencesContract))
            {
                string dll = Path.GetFileNameWithoutExtension(dllPath);
                PluginLoader<ITaskInterface> loader = LoaderOf(dllPath);
                classes.AddRange(loader.GetClasses().Select(name => new PackageClassTarget(identifier, name) { Dll = dll }));
            }
            return classes;
        }

        /// <summary>
        /// The context of [dllPath], loaded the first time. WPF finds the markup of a view by the name
        /// and the version of its assembly : two versions of a package with the same assembly version
        /// would show each other's views, so the second one is refused.
        /// </summary>
        private PluginLoader<ITaskInterface> LoaderOf(string dllPath)
        {
            lock (_loaders)
            {
                if (_loaders.TryGetValue(dllPath, out PluginLoader<ITaskInterface>? loader))
                    return loader;

                AssemblyName name = AssemblyName.GetAssemblyName(dllPath);
                Assembly? clashing = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(x => x.FullName == name.FullName && !string.Equals(x.Location, dllPath, StringComparison.OrdinalIgnoreCase));
                if (clashing != null)
                    throw new InvalidOperationException(
                        $"[{name.FullName}] is already loaded from another package ({clashing.Location}) : each version of a package of interfaces needs an assembly version of its own.");

                loader = new PluginLoader<ITaskInterface>(dllPath, isCollectible: false);
                _loaders.Add(dllPath, loader);
                return loader;
            }
        }

        /// <summary>Whether [dllPath] references the contract of the interfaces, read without loading it.</summary>
        private static bool ReferencesContract(string dllPath)
        {
            using FileStream stream = File.OpenRead(dllPath);
            using PEReader reader = new(stream);
            if (!reader.HasMetadata)
                return false;

            MetadataReader metadata = reader.GetMetadataReader();
            return metadata.AssemblyReferences
                .Select(metadata.GetAssemblyReference)
                .Any(reference => metadata.GetString(reference.Name) == ContractAssembly);
        }
    }
}
