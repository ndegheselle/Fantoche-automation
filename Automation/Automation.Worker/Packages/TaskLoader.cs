using System.Reflection;
using System.Runtime.Loader;
using Automation.Plugins.Shared;
using Automation.Shared.Data;

namespace Automation.Worker.Packages;

public class PluginLoader<TPlugin>: AssemblyLoadContext, IDisposable where TPlugin : class
{
    private readonly string _pluginPath;
    private readonly AssemblyDependencyResolver _resolver;

    public PluginLoader(string pluginPath) : base(true)
    {
        _pluginPath = pluginPath;
        _resolver = new AssemblyDependencyResolver(_pluginPath);
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        // Force shared assemblies to load from the default context
        if (assemblyName.Name == "System.Runtime" ||
            assemblyName.Name?.StartsWith("System.") == true)
            return null; // Fall back to default context
        
        // Let the resolver find the dependency path
        var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
        if (assemblyPath != null) return LoadFromAssemblyPath(assemblyPath);
        return null;
    }

    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        // Handle native dependencies
        var libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        if (libraryPath != null) return LoadUnmanagedDllFromPath(libraryPath);

        return IntPtr.Zero;
    }

    public TPlugin CreateInstance(string className)
    {
        // Load the main assembly
        Assembly assembly = LoadFromAssemblyPath(_pluginPath);
        Type type = assembly.GetType(className) ?? throw new Exception($"Could not get type [{className}].");

        if (!typeof(TPlugin).IsAssignableFrom(type))
            throw new Exception($"Type [{className}] does not implement [{nameof(TPlugin)}] interface.");

        TPlugin instance = (Activator.CreateInstance(type) as TPlugin)
                          ?? throw new Exception($"Failed to create instance of type [{className}].");
        return instance;
    }

    /// <summary>
    /// Get all types that implements <see cref="TPlugin"/>
    /// </summary>
    /// <returns></returns>
    public IEnumerable<Type> GetTypes()
    {
        Assembly assembly = LoadFromAssemblyPath(_pluginPath);
        return assembly.GetTypes()
            .Where(t => typeof(TPlugin).IsAssignableFrom(t) && t is { IsInterface: false, IsAbstract: false });
    }

    public IEnumerable<ClassIdentifier> GetClasses()
    {
        return GetTypes().Select(t => new ClassIdentifier(_pluginPath, t.FullName ?? ""));
    }

    public void Dispose() => Unload();
}

public class TaskLoader : PluginLoader<ITask>
{
    public TaskLoader(string pluginPath) : base(pluginPath)
    {}
}