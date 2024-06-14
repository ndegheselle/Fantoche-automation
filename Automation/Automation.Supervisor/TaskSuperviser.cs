using Automation.Plugins.Base;

namespace Automation.Supervisor
{
    /// <summary>
    /// Handle DLL resolution
    /// Handle load balancing
    /// Execute tasks with a TaskWorker
    /// </summary>
    public class TaskSuperviser
    {
        public List<Type> AvailableClasses { get; private set; } = new List<Type>();

        public List<Type> RefreshClassesFromFolderDlls(string dllsFolderPaths)
        {
            // Load all DLLs from the specified folder and get all classes that implement ITask
            AvailableClasses.Clear();

            // Get all DLLs from the specified folder
            var dlls = System.IO.Directory.GetFiles(dllsFolderPaths, "*.dll");
            foreach ( var dll in dlls)
            {
                var assembly = System.Reflection.Assembly.LoadFrom(dll);
                var types = assembly.GetTypes();
                foreach (var type in types)
                {
                    if (type.GetInterface(nameof(ITask)) != null)
                    {
                        AvailableClasses.Add(type);
                    }
                }
            }

            return AvailableClasses;
        }
    }
}
