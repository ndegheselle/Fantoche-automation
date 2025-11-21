using System.Diagnostics.Contracts;

namespace Automation.Plugins.Shared
{
    public enum EnumTaskNotificationState
    {
        Info,
        Warning,
        Error,
        Success
    }

    public struct TaskNotification
    {
        public TaskNotification()
        {}

        public EnumTaskNotificationState State { get; set; } = EnumTaskNotificationState.Info;
        public string Message { get; set; } = "";
    }

    public class TaskConnector
    {
        public Type? Type { get; set; }
    }
    
    public interface ITask
    {
        public TaskConnector? Input { get; }
        
        // XXX : should handle passing previous to output, pretty complexe to generate schema with that
        public TaskConnector? Output { get; }

        /// <summary>
        /// Execute the task asynchronously and return the result state of the task.
        /// </summary>
        /// <param name="parameters">Context of execution of the task, the context can be modified by the task to pass data to next tasks</param>
        /// <param name="progress"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public Task<object?> DoAsync(object parameters, IProgress<TaskNotification>? progress = null, CancellationToken? cancellation = null);
    }

    public abstract class BaseTask<TInput, TOutput> : ITask
    {
        public TaskConnector? Input { get; } = new TaskConnector() {Type =  typeof(TInput)};
        public TaskConnector? Output { get; } = new TaskConnector() {Type =  typeof(TOutput)};
        
        public async Task<object?> DoAsync(object parameters, IProgress<TaskNotification>? progress = null, CancellationToken? cancellation = null)
        {
            if (parameters is not TInput input)
                throw new ArgumentException($"Parameters are not of expected type '{Input!.Type}'.", nameof(parameters));

            return await DoAsync(input, progress, cancellation);
        }

        public abstract Task<TOutput> DoAsync(TInput parameters, IProgress<TaskNotification>? progress = null, CancellationToken? cancellation = null);
    }
    
    public abstract class BaseTask<TInput> : ITask
    {
        public TaskConnector? Input { get; } = new TaskConnector() {Type =  typeof(TInput)};
        public TaskConnector? Output { get; } = new TaskConnector();

        public async Task<object?> DoAsync(object parameters, IProgress<TaskNotification>? progress = null, CancellationToken? cancellation = null)
        {
            if (parameters is not TInput input)
                throw new ArgumentException($"Parameters are not of expected type '{Input!.Type}'.", nameof(parameters));
            await DoAsync(input, progress, cancellation);
            return null;
        }

        public abstract Task DoAsync(TInput parameters, IProgress<TaskNotification>? progress = null, CancellationToken? cancellation = null);
    }

    public abstract class BaseTaskOutputless<TInput> : ITask
    {
        public TaskConnector? Input { get; } = new TaskConnector() {Type =  typeof(TInput)};
        public TaskConnector? Output { get; } = null;

        public async Task<object?> DoAsync(object parameters, IProgress<TaskNotification>? progress = null, CancellationToken? cancellation = null)
        {
            if (parameters is not TInput input)
                throw new ArgumentException($"Parameters are not of expected type '{Input!.Type}'.", nameof(parameters));
            await DoAsync(input, progress, cancellation);
            return null;
        }

        public abstract Task DoAsync(TInput parameters, IProgress<TaskNotification>? progress = null, CancellationToken? cancellation = null);
    }
}
