using Fantoche.Shared.Base;
using Fantoche.Shared.Services;

namespace Fantoche.Shared.Remote;

/// <summary>
/// The failures of the services as they cross the HTTP API of the supervisor : named by the type of
/// their exception, so the application throws the same one as if the service ran in its process.
/// </summary>
public static class SupervisorErrors
{
    /// <summary>Where the errors of a <see cref="ValidationException"/> are kept in a problem.</summary>
    public const string ValidationErrors = "errors";

    public static string TypeOf(Exception exception) => exception.GetType().Name;

    /// <summary>
    /// The exception a problem of [type] stands for, a plain one for an unexpected failure.
    /// </summary>
    public static Exception ToException(string? type, string message, Dictionary<string, List<string>>? errors) => type switch
    {
        nameof(ValidationException) => new ValidationException(errors),
        nameof(PackageValidationException) => new PackageValidationException(message),
        nameof(ExecutionException) => new ExecutionException(message),
        nameof(KeyNotFoundException) => new KeyNotFoundException(message),
        nameof(InvalidOperationException) => new InvalidOperationException(message),
        _ => new Exception(message),
    };
}
