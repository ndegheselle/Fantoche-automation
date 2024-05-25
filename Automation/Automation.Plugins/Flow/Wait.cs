using Automation.Plugins.Base;

namespace Automation.Plugins.Flow
{
    public class  WaitDelay : ITask
    {
        public dynamic? Context { get; set; }
        public event EventHandler<TaskProgress>? Progress;

        public Dictionary<string, Type> InputsDefinition { get; private set; }
        public Dictionary<string, Type> OutputsDefinition { get; private set; }

        public Task<EnumTaskStatus> Start(Dictionary<string, dynamic> inputs)
        {
            throw new NotImplementedException();
        }
    }

    public class WaitAllTasks : ITask
    {
        public dynamic? Context { get; set; }
        public event EventHandler<TaskProgress>? Progress;

        public Dictionary<string, Type> InputsDefinition { get; private set; }
        public Dictionary<string, Type> OutputsDefinition { get; private set; }

        public Task<EnumTaskStatus> Start(Dictionary<string, dynamic> inputs)
        {
            throw new NotImplementedException();
        }
    }
}
