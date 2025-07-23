using Automation.Dal.Models;
using Automation.Shared.Data;
using Automation.Worker.Control.Flow;

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
            Id = Guid.Parse("00000000-0000-0000-0000-000000000002")
        };

        public static Dictionary<ClassIdentifier, Type> Availables { get; } = new Dictionary<ClassIdentifier, Type>()
        {
            { StartTask.Identifier, typeof(StartTask) }
        };
    }
}
