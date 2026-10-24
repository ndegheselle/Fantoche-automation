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
        /// Nothing runs it anymore, it waits to be resumed : paused by the user (the nodes running
        /// were let to finish), or cut short by a crash or a lost worker (the nodes running run again
        /// once resumed). The output of the run tells which.
        /// </summary>
        Suspended = 64,
        /// <summary>
        /// A user interface task waiting for the answer of the user, or a run holding one. Nothing
        /// runs it meanwhile : the answer resumes the run.
        /// </summary>
        AwaitingUser = 128,
        /// <summary>
        /// Nothing runs the task anymore : it is over, or waits to be resumed
        /// </summary>
        Finished = Completed | Failed | Canceled | Suspended | AwaitingUser
    }
}
