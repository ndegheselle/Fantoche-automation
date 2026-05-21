using System.Text.Json.Serialization;

namespace Automation.Shared.Data.Execution
{
    /// <summary>
    /// Base class for a task target
    /// </summary>
    [JsonDerivedType(typeof(ClassTarget), "class")]
    [JsonDerivedType(typeof(PackageClassTarget), "package")]
    public abstract class TaskTarget
    { }

    /// <summary>
    /// Class task target, a simple task target is internal to the automation project
    /// </summary>
    public class ClassTarget : TaskTarget
    {
        public string ClassFullName { get; set; } = "";
    }

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