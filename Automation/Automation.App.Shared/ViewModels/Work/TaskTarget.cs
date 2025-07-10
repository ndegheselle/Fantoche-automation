using Automation.Shared.Data;
using System.Text.Json.Serialization;

namespace Automation.App.Shared.ViewModels.Work
{
    [JsonDerivedType(typeof(ClassTarget), "class")]
    [JsonDerivedType(typeof(PackageClassTarget), "package")]
    public abstract class TaskTarget : ITaskTarget
    { }

    public class ClassTarget : IClassTarget
    {
        public ClassIdentifier Class { get; set; }
        public ClassTarget(ClassIdentifier targetClass)
        {
            Class = targetClass;
        }
    }

    public class PackageClassTarget : ClassTarget, IPackageClassTarget
    {
        public PackageIdentifier Identifier { get; set; }
        public PackageClassTarget() : base(new ClassIdentifier())
        {
            Identifier = new PackageIdentifier();
        }

        public PackageClassTarget(PackageIdentifier identifier, ClassIdentifier targetClass) : base(targetClass)
        {
            Identifier = identifier;
        }
    }
}
