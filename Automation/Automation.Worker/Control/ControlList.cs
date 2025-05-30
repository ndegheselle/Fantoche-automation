using Automation.Shared.Data;
using Automation.Worker.Control.Flow;

namespace Automation.Worker.Control
{
    public static class ControlList
    {
        public static Dictionary<EnumControlTaskType, Type> AvailablesTasks { get; } = new Dictionary<EnumControlTaskType, Type>()
        {
            { EnumControlTaskType.Start, typeof(StartTask) }
        };
    }
}
