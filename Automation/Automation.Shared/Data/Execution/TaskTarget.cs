using System.Text.Json.Serialization;

namespace Automation.Shared.Data.Execution
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