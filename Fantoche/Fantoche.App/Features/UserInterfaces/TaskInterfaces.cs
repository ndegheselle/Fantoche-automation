using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Windows;
using Fantoche.Plugins.Shared;
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
        private static readonly string ContractNamespace = typeof(ITaskInterface).Namespace!;
        private static readonly string ContractName = typeof(ITaskInterface).Name;

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

        /// <summary>The interfaces [id] [version] holds, each with the task it edits.</summary>
        public async Task<List<TaskInterfaceClass>> GetInterfacesAsync(string id, Version version)
        {
            PackageIdentifier identifier = new() { Id = id, Version = version };
            IEnumerable<string> dllPaths = await _packages.DownloadPackageAsync(id, version);

            List<TaskInterfaceClass> interfaces = [];
            // The assembly of the tasks a package carries isn't loaded on its own : only the
            // interfaces are, the tasks being loaded along in their context.
            foreach (string dllPath in dllPaths.Where(DeclaresInterface))
            {
                string dll = Path.GetFileNameWithoutExtension(dllPath);
                PluginLoader<ITaskInterface> loader = LoaderOf(dllPath);
                foreach (Type type in loader.GetTypes())
                {
                    PackageClassTarget target = new(identifier, type.FullName ?? "") { Dll = dll };
                    string? taskClass = type.GetCustomAttribute<TaskInterfaceAttribute>()?.Task.FullName;
                    interfaces.Add(new TaskInterfaceClass(target, taskClass));
                }
            }
            return interfaces;
        }

        /// <summary>
        /// The view [taskInterface] shows with [context]. An interface hands it as an object, its
        /// contract being free of WPF : anything but a WPF element can't be shown.
        /// </summary>
        /// <exception cref="InvalidOperationException">The view isn't a WPF element.</exception>
        public static FrameworkElement ViewOf(ITaskInterface taskInterface, TaskInterfaceContext context)
        {
            object view = taskInterface.Show(context);
            return view as FrameworkElement
                ?? throw new InvalidOperationException($"[{taskInterface.GetType().FullName}] shows a {view?.GetType().Name ?? "null"} rather than a WPF element.");
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

        /// <summary>
        /// Whether a class of [dllPath] implements <see cref="ITaskInterface"/>, read without loading
        /// it : the assembly of the tasks a package carries references the same contract, and loading
        /// it on its own would keep a second copy of it for nothing.
        /// </summary>
        private static bool DeclaresInterface(string dllPath)
        {
            using FileStream stream = File.OpenRead(dllPath);
            using PEReader reader = new(stream);
            if (!reader.HasMetadata)
                return false;

            MetadataReader metadata = reader.GetMetadataReader();
            foreach (TypeDefinitionHandle typeHandle in metadata.TypeDefinitions)
            {
                TypeDefinition type = metadata.GetTypeDefinition(typeHandle);
                foreach (InterfaceImplementationHandle implementationHandle in type.GetInterfaceImplementations())
                {
                    EntityHandle implemented = metadata.GetInterfaceImplementation(implementationHandle).Interface;
                    if (implemented.Kind != HandleKind.TypeReference)
                        continue;

                    TypeReference reference = metadata.GetTypeReference((TypeReferenceHandle)implemented);
                    if (metadata.GetString(reference.Name) == ContractName && metadata.GetString(reference.Namespace) == ContractNamespace)
                        return true;
                }
            }
            return false;
        }
    }
}
