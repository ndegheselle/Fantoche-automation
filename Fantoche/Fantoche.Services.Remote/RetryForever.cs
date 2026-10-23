using Microsoft.AspNetCore.SignalR.Client;

namespace Fantoche.Services.Remote;

/// <summary>
/// The application keeps listening however long the supervisor takes to come back.
/// </summary>
internal sealed class RetryForever : IRetryPolicy
{
    public TimeSpan? NextRetryDelay(RetryContext retryContext) => TimeSpan.FromSeconds(5);
}
