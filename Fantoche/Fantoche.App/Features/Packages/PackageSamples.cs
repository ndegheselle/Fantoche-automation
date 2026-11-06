using Fantoche.App.Features.UserInterfaces;
using Fantoche.Shared.Data.Execution;
using Fantoche.Shared.Samples;
using Fantoche.Shared.Services;
using Fantoche.Worker;

namespace Fantoche.App.Features.Packages
{
    /// <summary>
    /// The samples a package comes with, created once it is added (see <see cref="ExampleSamples"/>) :
    /// the application loads the tasks for their schemas, and the interfaces, which only it can load.
    /// </summary>
    public sealed class PackageSamples
    {
        private readonly IScopedService _scoped;
        private readonly IPackagesService _packages;
        private readonly LocalWorker _worker;
        private readonly TaskInterfaces _interfaces;

        public PackageSamples(IScopedService scoped, IPackagesService packages, LocalWorker worker, TaskInterfaces interfaces)
        {
            _scoped = scoped;
            _packages = packages;
            _worker = worker;
            _interfaces = interfaces;
        }

        /// <summary>
        /// Create the samples of [package] : what was created, to tell the user, null when the
        /// package has none.
        /// </summary>
        public async Task<string?> CreateAsync(PackageInfos package)
        {
            // Its symbols only make a package debuggable : there is nothing to create for them.
            if (package.IsSymbols)
                return null;

            // A package always has its version, only a task target goes without one.
            Version version = package.Identifier.Version!;
            return package.Identifier.Id switch
            {
                ExampleSamples.PackageId => await CreateExamplesAsync(version),
                ExampleSamples.InterfacesPackageId => await AttachInterfacesAsync(ExampleSamples.InterfacesPackageId, version),
                _ => null,
            };
        }

        private async Task<string> CreateExamplesAsync(Version version)
        {
            List<ClassTarget> classes = await _packages.GetClassesAsync(ExampleSamples.PackageId, version);
            SamplesCreated created = await ExampleSamples.CreateAsync(_scoped, classes, _worker.LoadTaskAsync);

            List<string> lines = [];
            lines.Add(created.Elements.Count == 0
                ? $"The samples were already in the scope '{ExampleSamples.PackageId}'."
                : $"Created in the scope '{ExampleSamples.PackageId}' : {string.Join(", ", created.Elements)}.");
            if (created.MissingClasses.Count > 0)
                lines.Add($"The workflow '{ExampleSamples.WorkflowName}' needs tasks the package doesn't hold : {string.Join(", ", created.MissingClasses)}.");

            // Added before the tasks, the interfaces had nothing to be given to.
            Version? interfacesVersion = (await _packages.GetVersionsAsync(ExampleSamples.InterfacesPackageId)).FirstOrDefault();
            if (interfacesVersion != null)
                lines.Add(await AttachInterfacesAsync(ExampleSamples.InterfacesPackageId, interfacesVersion));
            return string.Join("\n", lines);
        }

        private async Task<string> AttachInterfacesAsync(string id, Version version)
        {
            List<TaskInterfaceClass> interfaces = await _interfaces.GetInterfacesAsync(id, version);
            int attached = await ExampleSamples.AttachInterfacesAsync(_scoped, interfaces);
            return attached == 0
                ? "No task without an interface runs the tasks of its interfaces : add the example package to create them."
                : $"Its interfaces were linked to {attached} tasks, under which the tree shows them.";
        }
    }
}
