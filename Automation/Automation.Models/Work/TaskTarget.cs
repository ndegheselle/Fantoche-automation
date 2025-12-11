using Automation.Shared.Data;
using System.Text.Json.Serialization;

namespace Automation.Models.Work
{
    [JsonDerivedType(typeof(ClassTarget), "class")]
    [JsonDerivedType(typeof(PackageClassTarget), "package")]
    public abstract class TaskTarget
    { }

    public class ClassTarget : TaskTarget
    {
        public string ClassFullName { get; set; } = "";
    }

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