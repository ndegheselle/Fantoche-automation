using Automation.Shared.Data;
using Automation.Worker.Control.Flow;

namespace Automation.Worker.Control
{
    /// <summary>
    /// List of all available control tasks types
    /// </summary>
    public static class ControlTaskList
    {
        public static Dictionary<Guid, Type> Availables { get; } = new Dictionary<Guid, Type>()
        {
            { GraphControls.Start, typeof(StartTask) }
        };
    }
}
