using System.Xml.Linq;

namespace Automation.Shared.Data
{
    public class PackageIdentifier
    {
        public string Id { get; set; } = "";
        public Version Version { get; set; } = new Version();
    }

    public class PackageInfos
    {
        public PackageIdentifier Identifier { get; set; } =  new PackageIdentifier();
        public string Description { get; set; } = "";
    }

    public class ClassIdentifier
    {
        public string Dll { get; set; }
        public string Name { get; set; }

        public ClassIdentifier()
        {
            Dll = string.Empty;
            Name = string.Empty;
        }

        public ClassIdentifier(string dll, string name)
        {
            Dll = dll;
            Name = name;
        }

        public override bool Equals(object? other)
        {
            if (other is ClassIdentifier identifier)
                return Dll == identifier.Dll && Name == identifier.Name;
            return false;
        }

        public override int GetHashCode()
        {
            return (Dll, Name).GetHashCode();
        }
    }

    public abstract class TaskTarget
    { }

    public class ClassTarget : TaskTarget
    {
        public ClassIdentifier Class { get; set; }
        public ClassTarget(ClassIdentifier targetClass)
        {
            Class = targetClass;
        }
    }

    public class PackageClassTarget : ClassTarget
    {
        public PackageIdentifier Identifier { get; set; }
        public PackageClassTarget(PackageIdentifier identifier, ClassIdentifier targetClass) : base(targetClass)
        {
            Identifier = identifier;
        }
    }
}