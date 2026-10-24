namespace Fantoche.Shared.Data.Execution
{
    /// <summary>
    /// State of a task instance
    /// </summary>
    [Flags]
    public enum EnumTaskState
    {
        /// <summary>
        /// Waiting for a worker to execute the task
        /// </summary>
        Pending = 1,
        /// <summary>
        /// Task is waiting on another task, a manual action or a timer
        /// </summary>
        Waiting = 2,
        /// <summary>
        /// Task is progressing
        /// </summary>
        Progressing = 4,
        /// <summary>
        /// Task is completed
        /// </summary>
        Completed = 8,
        /// <summary>
        /// Task failed
        /// </summary>
        Failed = 16,
        /// <summary>
        /// Task is canceled
        /// </summary>
        Canceled = 32,
        /// <summary>
        /// Stopped by a crash or a lost worker, nothing runs it anymore : a run in this state can be
        /// resumed, its interrupted nodes running again.
        /// </summary>
        Interrupted = 64,
        /// <summary>
        /// Paused by the user : the nodes that were running were let to finish, no other started.
        /// A run in this state can be resumed.
        /// </summary>
        Paused = 128,
        /// <summary>
        /// Nothing runs the task anymore : it is over, or waits to be resumed
        /// </summary>
        Finished = Completed | Failed | Canceled | Interrupted | Paused
    }
}
