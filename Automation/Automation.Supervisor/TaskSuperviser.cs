using Automation.Plugins.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Automation.Supervisor
{
    public class  TaskSupervisorParams
    {
        public List<string> DllsFolderPaths { get; set; }
    }

    /// <summary>
    /// Handle DLL resolution
    /// Handle load balancing
    /// Execute tasks with a TaskWorker
    /// </summary>
    public class TaskSuperviser
    {
        private readonly TaskSupervisorParams Params;
        public List<Type> AvailableClasses { get; private set; }

        public TaskSuperviser(TaskSupervisorParams taskSupervisorParams)
        {
            Params = taskSupervisorParams;
            AvailableClasses = LoadClassesFromDlls(Params.DllsFolderPaths);
        }

        private List<Type> LoadClassesFromAssembly(string assemblyName)
        {
            List<Type> availableTasks = new List<Type>();
            var assembly = System.Reflection.Assembly.Load(assemblyName);
            foreach (var type in assembly.GetTypes())
            {
                if (type.IsAssignableFrom(typeof(ITask)))
                    availableTasks.Add(type);
            }
            return availableTasks;
        }

        private List<Type> LoadClassesFromDlls(List<string> dllsFolderPaths)
        {
            // Load all DLLs from the specified folder and get all classes that implement ITask
            List<Type> availableTasks = new List<Type>();
            foreach (var dllPath in dllsFolderPaths)
            {
                var dll = System.Reflection.Assembly.LoadFile(dllPath);
                
                foreach (var type in dll.GetTypes())
                {
                    if (type.IsAssignableFrom(typeof(ITask)))
                        availableTasks.Add(type);
                }
            }
            return availableTasks;
        }
    }
}
