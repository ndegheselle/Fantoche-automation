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
        public ClassIdentifier TargetClass { get; set; }
        public ClassTarget(ClassIdentifier targetClass)
        {
            TargetClass = targetClass;
        }
    }

    public class PackageClassTarget : ClassTarget
    {
        public PackageIdentifier Package { get; set; }

        public PackageClassTarget() : base(new ClassIdentifier())
        {
            Package = new PackageIdentifier();
        }

        public PackageClassTarget(PackageIdentifier identifier, ClassIdentifier targetClass) : base(targetClass)
        {
            Package = identifier;
        }
    }
}