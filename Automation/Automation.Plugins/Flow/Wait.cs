using Automation.Plugins.Base;

namespace Automation.Plugins.Flow
{
    public class  WaitDelay : ITask
    {
        public dynamic? Context { get; set; }

        public Dictionary<string, TaskEndpoint> Inputs { get; private set; }
        public Dictionary<string, TaskEndpoint> Outputs { get; private set; }

        public Task<bool> Start()
        {
            throw new NotImplementedException();
        }
    }

    public class WaitAllTasks : ITask
    {
        public dynamic? Context { get; set; }

        public Dictionary<string, TaskEndpoint> Inputs { get; private set; }
        public Dictionary<string, TaskEndpoint> Outputs { get; private set; }

        public Task<bool> Start()
        {
            throw new NotImplementedException();
        }
    }
}
