using System.Reflection;
using Fantoche.Plugins.Shared;
using Newtonsoft.Json.Linq;

namespace Fantoche.Worker.UserInterfaces;

/// <summary>
/// The methods of a task its page calls, see <see cref="PageMethodAttribute"/>.
/// </summary>
public static class PageMethods
{
    /// <summary>
    /// Call the page method [name] of [task] with [args], and hand back what it returns.
    /// </summary>
    /// <exception cref="ArgumentException">[task] has no page method [name], or [args] doesn't fit it.</exception>
    public static async Task<JToken?> CallAsync(object task, string name, JToken? args, ITaskRuntime runtime, CancellationToken cancellation)
    {
        MethodInfo method = task.GetType()
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(x => x.Name == name && x.IsDefined(typeof(PageMethodAttribute)))
            ?? throw new ArgumentException($"[{name}] isn't a page method of the task.");

        object?[] arguments = ArgumentsOf(method, args, runtime, cancellation);
        object? returned;
        try
        {
            returned = method.Invoke(task, arguments);
        }
        catch (TargetInvocationException exception) when (exception.InnerException != null)
        {
            throw exception.InnerException;
        }

        object? result = await ResultOfAsync(method, returned);
        return result == null ? null : JToken.FromObject(result);
    }

    /// <summary>The runtime and the cancellation where asked for, [args] for the one value it takes.</summary>
    private static object?[] ArgumentsOf(MethodInfo method, JToken? args, ITaskRuntime runtime, CancellationToken cancellation)
    {
        bool isValueTaken = false;
        List<object?> arguments = [];
        foreach (ParameterInfo parameter in method.GetParameters())
        {
            if (parameter.ParameterType == typeof(CancellationToken))
            {
                arguments.Add(cancellation);
                continue;
            }
            if (parameter.ParameterType == typeof(ITaskRuntime))
            {
                arguments.Add(runtime);
                continue;
            }
            if (isValueTaken)
                throw new ArgumentException($"The page method [{method.Name}] takes more than one value.");

            isValueTaken = true;
            arguments.Add(args == null || args.Type == JTokenType.Null ? null : args.ToObject(parameter.ParameterType));
        }
        return [.. arguments];
    }

    /// <summary>What [returned] by [method] comes to, a task being awaited.</summary>
    private static async Task<object?> ResultOfAsync(MethodInfo method, object? returned)
    {
        if (returned is not Task task)
            return returned;

        await task;
        // Read from what the method declares : the task of an async method returning nothing is a
        // Task<T> of an internal type at runtime.
        bool hasResult = method.ReturnType.IsGenericType && method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>);
        return hasResult ? method.ReturnType.GetProperty(nameof(Task<object>.Result))!.GetValue(task) : null;
    }
}
