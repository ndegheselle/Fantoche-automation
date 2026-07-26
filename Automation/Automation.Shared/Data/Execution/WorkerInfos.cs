namespace Automation.Shared.Data.Execution
{
    /// <summary>
    /// State of a worker as described in the server readme (available, working, down, updating).
    /// </summary>
    public enum EnumWorkerState
    {
        /// <summary>
        /// The worker is up and has room to take new tasks.
        /// </summary>
        Available,
        /// <summary>
        /// The worker is up and currently executing tasks.
        /// </summary>
        Working,
        /// <summary>
        /// The worker missed its heartbeat and is considered down.
        /// </summary>
        Down,
        /// <summary>
        /// The worker is updating (package/runtime) and not taking tasks.
        /// </summary>
        Updating
    }

    /// <summary>
    /// UI facing description of a worker. Aggregates the realtime data (state, heartbeat, current
    /// load) that only makes sense while the worker is up with its static registration parameters
    /// (parallelism, queue size, ...).
    /// </summary>
    public class WorkerInfos
    {
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Friendly name of the worker.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Machine the worker runs on (hostname / hardware id).
        /// </summary>
        public string MachineName { get; set; } = string.Empty;

        public EnumWorkerState State { get; set; } = EnumWorkerState.Available;

        /// <summary>
        /// Number of tasks the worker can run in parallel.
        /// </summary>
        public int MaxParallelTasks { get; set; }

        /// <summary>
        /// Number of tasks currently running on the worker.
        /// </summary>
        public int RunningTasks { get; set; }

        /// <summary>
        /// Number of tasks currently queued on the worker.
        /// </summary>
        public int QueuedTasks { get; set; }

        /// <summary>
        /// Maximum number of tasks the worker keeps in its queue.
        /// </summary>
        public int MaxQueueSize { get; set; }

        /// <summary>
        /// Version of the worker runtime.
        /// </summary>
        public string Version { get; set; } = string.Empty;

        /// <summary>
        /// Last time the worker sent a heartbeat.
        /// </summary>
        public DateTime LastHeartbeat { get; set; }
    }
}
