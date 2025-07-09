using Automation.Shared.Data;

namespace Automation.App.Shared.ViewModels.Work
{
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
        public PackageClassTarget(PackageIdentifier identifier, ClassIdentifier targetClass) : base(targetClass)
        {
            Identifier = identifier;
        }
    }
}
