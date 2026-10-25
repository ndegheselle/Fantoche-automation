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

        /// <summary>
        /// Whether the class can ask the user through a page while it runs (an <c>IUserInterfaceTask</c>),
        /// read from the package when the class is picked so the editor tells it without loading it.
        /// </summary>
        public bool IsUserInterface { get; set; }
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