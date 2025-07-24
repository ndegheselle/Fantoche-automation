using Automation.Dal.Models;
using Automation.Shared.Data;

namespace Automation.Worker.Control
{
    /// <summary>
    /// List of all available control tasks types
    /// Should also add them to the database seeder <see cref="Automation.Supervisor.Api.Database.DatabaseSeeder"/>
    /// </summary>
    public static class ControlsTasks
    {
        public static Scope ControlScope = new Scope("Controls", [Scope.ROOT_SCOPE_ID])
        {
            Id = Guid.Parse("00000000-0000-0000-0000-100000000000")
        };

        public static Dictionary<ClassIdentifier, AutomationControl> Availables { get; } = new Dictionary<ClassIdentifier, AutomationControl>();
        public static Dictionary<Guid, AutomationControl> AvailablesById { get; } = new Dictionary<Guid, AutomationControl>();

        public static void Register<T>(AutomationControl task)
        {
            if (AvailablesById.ContainsKey(task.Id))
                throw new Exception($"The key '{task.Id}' is already registered by task '{AvailablesById[task.Id].Metadata.Name}'.");

            ClassTarget target = new ClassTarget(new ClassIdentifier() { Dll = "internal.controls", Name = typeof(T).Name, });
            task.Target = target;
            task.Type = typeof(T);
            ControlScope.AddChild(task);
            Availables.Add(target.Class, task);
            AvailablesById.Add(task.Id, task);
        }
    }
}
