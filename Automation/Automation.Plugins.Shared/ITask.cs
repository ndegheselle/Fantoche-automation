namespace Automation.Plugins.Shared
{
    public enum EnumTaskProgressState
    {
        Info,
        Warning,
        Error,
        Sucess
    }

    public class TaskProgress
    {
        public EnumTaskProgressState State { get; set; }
        public string Message { get; set; } = "";
    }

    public interface ITask
    {
        /// <summary>
        /// Execute the task asynchronously and return the result state of the task.
        /// </summary>
        /// <param name="parameters">Context of execution of the task, the context can be modified by the task to pass data to next tasks</param>
        /// <param name="progress"></param>
        /// <returns></returns>
        public Task<object?> DoAsync(object parameters, IProgress<TaskProgress>? progress);
    }

    public interface ITask<TParameters, TResult> : ITask
    {
        public Type ParametersType => typeof(TParameters);
        public Type ResultType => typeof(TResult);

        public new  async Task<object?> DoAsync(object parameters, IProgress<TaskProgress>? progress)
        {
            if (parameters is not TParameters)
                throw new ArgumentException($"Parameters are not of expected type '{ParametersType}'.", nameof(parameters));

            return await DoAsync((TParameters)parameters, progress);
        }

        public Task<TResult> DoAsync(TParameters parameters, IProgress<TaskProgress>? progress);
    }
}
