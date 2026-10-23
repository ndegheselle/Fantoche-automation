using Fantoche.Services.Local;
using Microsoft.AspNetCore.Http.Features;

namespace Fantoche.Supervisor.Api.Data;

/// <summary>
/// The storage in transit between a caller and the worker running its execution, see
/// <see cref="Worker.Storage.IStorageTransit"/>.
/// </summary>
public static class TransitApi
{
    public static void Map(WebApplication app)
    {
        var transit = app.MapGroup("/transit/{executionId:guid}");

        transit.MapGet("/", (LocalStorageTransit service, Guid executionId) => service.GetItemsAsync(executionId));
        transit.MapDelete("/", (LocalStorageTransit service, Guid executionId) => service.ClearAsync(executionId));
        transit.MapPut("/{**item}", PutAsync);
        transit.MapGet("/{**item}", async (LocalStorageTransit service, Guid executionId, string item)
            => TypedResults.Stream(await service.OpenAsync(executionId, item), "application/octet-stream"));
    }

    /// <summary>
    /// A table or a file is as big as the data a task handles : the size of a request isn't
    /// limited.
    /// </summary>
    private static Task PutAsync(HttpContext context, LocalStorageTransit service, Guid executionId, string item)
    {
        var size = context.Features.GetRequiredFeature<IHttpMaxRequestBodySizeFeature>();
        size.MaxRequestBodySize = null;
        return service.PutAsync(executionId, item, context.Request.Body);
    }
}
