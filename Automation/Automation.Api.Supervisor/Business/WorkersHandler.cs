using Automation.Api.Shared;

namespace Automation.Api.Supervisor.Business
{
    public class WorkersHandler
    {
        public Dictionary<string, WorkerInstance> Workers { get; private set; } = [];

        public void RegisterWorker(WorkerInstance infos)
        {
            if (Workers.ContainsKey(infos.Id))
                throw new InvalidOperationException("Worker is already registered.");
            Workers.Add(infos.Id, infos);
        }

        public void UnregisterWorker(string workerId)
        {
            // Send info to worker ?
            Workers.Remove(workerId);
        }
    }
}
