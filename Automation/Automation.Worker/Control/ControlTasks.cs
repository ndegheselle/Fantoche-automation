using Automation.Models.Work;
using Automation.Shared.Data;
using Automation.Worker.Control.Flow;

namespace Automation.Worker.Control
{
    /// <summary>
    /// List of all available control tasks types
    /// Should also add them to the database seeder <see cref="Automation.Supervisor.Api.Database.DatabaseSeeder"/>
    /// </summary>
    public static class ControlTasks
    {
        public static Scope ControlScope = new Scope("Controls", [Scope.ROOT_SCOPE_ID])
        {
            Id = Guid.Parse("00000000-0000-0000-0000-100000000000")
        };

        public static Dictionary<string, AutomationControl> Availables { get; } = new Dictionary<string, AutomationControl>();
        public static Dictionary<Guid, AutomationControl> AvailablesById { get; } = new Dictionary<Guid, AutomationControl>();

        static ControlTasks()
        {
            Register<StartTask>(StartTask.AutomationTask);
            Register<EndTask>(EndTask.AutomationTask);
        }

        public static AutomationControl Register<T>(AutomationControl task) where T : ITaskControl
        {
            if (AvailablesById.ContainsKey(task.Id))
                throw new Exception($"The key '{task.Id}' is already registered by task '{AvailablesById[task.Id].Metadata.Name}'.");

            ClassTarget target = new ClassTarget() { ClassFullName = typeof(T).FullName ?? "" };
            task.Target = target;
            task.Type = typeof(T);
            ControlScope.AddChild(task);
            Availables.Add(target.ClassFullName, task);
            AvailablesById.Add(task.Id, task);
            return task;
        }
    }
}
