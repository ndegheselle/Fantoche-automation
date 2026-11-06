using System.Text.Json.Serialization;

namespace Fantoche.Shared.Data.Execution
{
    /// <summary>
    /// Class task target, a simple task target is internal to the automation project
    /// </summary>
    public class ClassTarget
    {
        public string ClassFullName { get; set; } = "";
        public string Dll { get; set; } = "";
    }

    /// <summary>
    /// An interface of a package, and the class of the task it edits (its <c>TaskInterfaceAttribute</c>),
    /// null when it doesn't say.
    /// </summary>
    public sealed record TaskInterfaceClass(PackageClassTarget Target, string? TaskClass);

    /// <summary>
    /// Package class target, the target class is contained in a separated package
    /// </summary>
    public class PackageClassTarget : ClassTarget
    {
        public PackageIdentifier Package { get; set; }
        public PackageClassTarget()
        {
            Package = new PackageIdentifier();
        }

        public PackageClassTarget(PackageIdentifier identifier, string classFullName)
        {
            Package = identifier;
            ClassFullName = classFullName;
        }
    }
}